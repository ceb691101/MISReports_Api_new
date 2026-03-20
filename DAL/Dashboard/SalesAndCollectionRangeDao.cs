using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Dashboard
{
    public class SalesAndCollectionRangeDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // ------------------------------------------------------------------ //
        // Connection test (Ordinary DB — both queries hit the ordinary DB)
        // ------------------------------------------------------------------ //
        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, useBulkConnection: false);
        }

        // ------------------------------------------------------------------ //
        // Public entry point
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Fetches sales &amp; collection data for a dynamic 8-cycle range.
        /// The range is derived as  [maxBillCycle - 7 .. maxBillCycle]
        /// where maxBillCycle comes from  SELECT MAX(bill_cycle) FROM receive_position
        /// (Ordinary DB).  Both ordinary (bill_type='O') and bulk (bill_type='B')
        /// rows are read from the same Ordinary database.
        /// </summary>
        public SalesAndCollectionRangeResult GetSalesAndCollectionRange()
        {
            var result = new SalesAndCollectionRangeResult();

            try
            {
                logger.Info("=== START GetSalesAndCollectionRange ===");

                using (var conn = _dbConnection.GetConnection(useBulkConnection: false))
                {
                    conn.Open();

                    // Step 1 – get the maximum bill cycle
                    int maxBillCycle = GetMaxBillCycle(conn);
                    int minBillCycle = maxBillCycle - 11;          // 12 cycles inclusive

                    result.MaxBillCycle = maxBillCycle;

                    logger.Info($"Bill cycle range: {minBillCycle} – {maxBillCycle}");

                    // Step 2 – ordinary rows (bill_type = 'O')
                    result.OrdinaryData = GetByBillType(conn, minBillCycle, maxBillCycle, "O");
                    logger.Info($"Ordinary records fetched: {result.OrdinaryData.Count}");

                    // Step 3 – bulk rows (bill_type = 'B')
                    result.BulkData = GetByBillType(conn, minBillCycle, maxBillCycle, "B");
                    logger.Info($"Bulk records fetched: {result.BulkData.Count}");
                }

                logger.Info("=== END GetSalesAndCollectionRange (Success) ===");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetSalesAndCollectionRange");
                throw;
            }
        }

        // ------------------------------------------------------------------ //
        // Private helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the maximum bill_cycle value from receive_position (Ordinary DB).
        /// </summary>
        private int GetMaxBillCycle(OleDbConnection conn)
        {
            const string sql = "SELECT MAX(bill_cycle) FROM receive_position";

            try
            {
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    var scalar = cmd.ExecuteScalar();

                    if (scalar == null || scalar == DBNull.Value)
                    {
                        logger.Warn("MAX(bill_cycle) returned NULL; defaulting to 0");
                        return 0;
                    }

                    return Convert.ToInt32(scalar);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching MAX(bill_cycle) from receive_position");
                throw;
            }
        }

        /// <summary>
        /// Runs the parameterised collection/sales query for the given bill_type.
        /// </summary>
        /// <param name="conn">Open OleDb connection.</param>
        /// <param name="minCycle">Lower bound of the bill_cycle range (inclusive).</param>
        /// <param name="maxCycle">Upper bound of the bill_cycle range (inclusive).</param>
        /// <param name="billType">'O' for Ordinary customers, 'B' for Bulk customers.</param>
        private List<SalesAndCollectionModel> GetByBillType(
            OleDbConnection conn,
            int minCycle,
            int maxCycle,
            string billType)
        {
            var rows = new List<SalesAndCollectionModel>();

            // OleDb uses positional '?' placeholders
            const string sql = @"
                SELECT   bill_cycle,
                         SUM(payments) AS collection,
                         SUM(mon_chg)  AS sales
                FROM     receive_position
                WHERE    bill_cycle  >= ?
                  AND    bill_cycle  <= ?
                  AND    bill_type    = ?
                GROUP BY bill_cycle
                ORDER BY bill_cycle";

            try
            {
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", minCycle);
                    cmd.Parameters.AddWithValue("?", maxCycle);
                    cmd.Parameters.AddWithValue("?", billType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new SalesAndCollectionModel
                            {
                                BillCycle = GetIntValue(reader, "bill_cycle"),
                                Collection = GetDecimalValue(reader, "collection"),
                                Sales = GetDecimalValue(reader, "sales"),
                                ErrorMessage = string.Empty
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error fetching sales & collection for bill_type='{billType}'");
                throw;
            }

            return rows;
        }

        // ------------------------------------------------------------------ //
        // Reader helpers (mirrors the pattern used across the codebase)
        // ------------------------------------------------------------------ //

        private int GetIntValue(OleDbDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid int format in column '{column}'");
                return 0;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? 0m : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return 0m;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{column}'");
                return 0m;
            }
        }
    }
}