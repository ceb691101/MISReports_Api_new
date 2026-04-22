using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;

namespace MISReports_Api.DAL.Dashboard
{
    public class SalesAndCollectionRangeDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string _posPaymentConnectionString;
        private readonly string _bankPaymentConnectionString;

        public SalesAndCollectionRangeDao()
        {
            _posPaymentConnectionString = GetRequiredConnectionString("InformixPosPayment");
            _bankPaymentConnectionString = GetRequiredConnectionString("InformixBankPayment");
        }

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

                // Return a rolling 7-day window ending on day-before-today.
                DateTime toDate = DateTime.Today.AddDays(-1);
                DateTime fromDate = toDate.AddDays(-6);

                result.MaxBillCycle = 0;

                result.OrdinaryData = GetOrdinarySalesAndCollectionByDateRange(fromDate, toDate);
                logger.Info($"Ordinary records fetched: {result.OrdinaryData.Count}");

                result.BulkData = GetBulkSalesAndCollectionByDateRange(fromDate, toDate);
                logger.Info($"Bulk records fetched: {result.BulkData.Count}");

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
                                Date = string.Empty,
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

        private List<SalesAndCollectionModel> GetOrdinarySalesAndCollectionByDateRange(DateTime fromDate, DateTime toDate)
        {
            return GetSalesAndCollectionByDateRange(fromDate, toDate, "O");
        }

        private List<SalesAndCollectionModel> GetBulkSalesAndCollectionByDateRange(DateTime fromDate, DateTime toDate)
        {
            return GetSalesAndCollectionByDateRange(fromDate, toDate, "B");
        }

        private List<SalesAndCollectionModel> GetSalesAndCollectionByDateRange(DateTime fromDate, DateTime toDate, string billType)
        {
            var dailyCollection = new Dictionary<DateTime, decimal>();
            var toDateExclusive = toDate.Date.AddDays(1);

            const string posCollectionSql = @"
                SELECT c.trans_date,
                       SUM(c.trans_amt) AS amount
                FROM cus_tran c, areas a
                WHERE c.area_code = a.area_code
                  AND c.bill_type = ?
                  AND c.trans_type = 0
                  AND c.trans_date >= ?
                  AND c.trans_date < ?
                GROUP BY 1
                ORDER BY 1";

            const string bankCashSql = @"
                SELECT b.cash_date,
                       SUM(b.paid_amount) AS amount
                FROM bank_paymast b, bankname c, areas p
                WHERE b.area_code = p.area_code
                  AND b.bank_code = c.bank_code
                  AND b.cash_date >= ?
                  AND b.cash_date < ?
                  AND b.bill_type = ?
                GROUP BY 1
                ORDER BY 1";

            const string cardCashSql = @"
                SELECT b.cash_date,
                       SUM(b.tot_amt) AS amount
                FROM crdtauth b, areas p
                WHERE b.area_code = p.area_code
                  AND b.cash_date >= ?
                  AND b.cash_date < ?
                  AND b.bill_type = ?
                GROUP BY 1
                ORDER BY 1";

            try
            {
                logger.Info($"=== START GetSalesAndCollectionByDateRange billType={billType}: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} ===");
                //logger.Info($"=== START GetSalesAndCollectionByDateRange billType={billType}: {fromDate:dd-MM-yyyy} to {toDate:dd-MM-yyyy} ===");

                using (var posConn = new OdbcConnection(_posPaymentConnectionString))
                {
                    posConn.Open();
                    AddDateAmountRowsFromOdbc(
                        conn: posConn,
                        sql: posCollectionSql,
                        destination: dailyCollection,
                        parameters: new object[] { billType, fromDate.Date, toDateExclusive });
                }

                using (var bankConn = new OdbcConnection(_bankPaymentConnectionString))
                {
                    bankConn.Open();

                    AddDateAmountRowsFromOdbc(
                        conn: bankConn,
                        sql: bankCashSql,
                        destination: dailyCollection,
                        parameters: new object[] { fromDate.Date, toDateExclusive, billType });

                    AddDateAmountRowsFromOdbc(
                        conn: bankConn,
                        sql: cardCashSql,
                        destination: dailyCollection,
                        parameters: new object[] { fromDate.Date, toDateExclusive, billType });
                }

                var rows = new List<SalesAndCollectionModel>();

                for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
                {
                   var amount = dailyCollection.TryGetValue(day, out var value) ? value : 0m;

                   rows.Add(new SalesAndCollectionModel
                   {
                       Date = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                       //Date = day.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                       // Keep contract-compatible int field using yyyyMMdd for daily series.
                       BillCycle = int.Parse(day.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                       //BillCycle = int.Parse(day.ToString("ddMMyyyy", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                       Collection = amount,
                       Sales = amount,
                       ErrorMessage = string.Empty
                   });
                }
                logger.Info($"=== END GetSalesAndCollectionByDateRange billType={billType} (rows: {rows.Count}) ===");
                return rows;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error in GetSalesAndCollectionByDateRange for billType='{billType}'");
                throw;
            }
        }

        private void AddDateAmountRowsFromOdbc(
            OdbcConnection conn,
            string sql,
            Dictionary<DateTime, decimal> destination,
            object[] parameters)
        {
            try
            {
                using (var cmd = new OdbcCommand(sql, conn))
                {
                    foreach (var parameter in parameters)
                        cmd.Parameters.AddWithValue("?", parameter);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var date = Convert.ToDateTime(reader[0]).Date;
                            var amount = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);

                            if (destination.ContainsKey(date))
                                destination[date] += amount;
                            else
                                destination[date] = amount;
                        }
                    }
                }
            }
            catch (OdbcException ex) when (IsNoRecordFound(ex.Message))
            {
                logger.Warn($"No records found for query/date range. Source skipped. Details: {ex.Message}");
            }
        }

        private static bool IsNoRecordFound(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            return message.IndexOf("no record found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetRequiredConnectionString(string key)
        {
            var setting = ConfigurationManager.ConnectionStrings[key];

            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
                throw new ConfigurationErrorsException($"{key} connection string is missing or empty in configuration.");

            return setting.ConnectionString;
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