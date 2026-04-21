using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.General.AreasPosition
{
    /// <summary>
    /// Data-access layer for the Areas Position report.
    ///
    /// Logic (ported from legacy VB):
    ///   1. Resolve the max bill_cycle for the selected area.
    ///   2. For every reader_code in that bill cycle / area:
    ///        charge(i)  = sum(kwh_charge + fuel_charge)  –  sum of NR transaction amounts
    ///        balance(i) = sum(crnt_balance)
    ///        ratio(i)   = balance(i) / charge(i)   [0 when charge = 0]
    ///        count(i)   = count(reader_code)
    /// </summary>
    public class AreasPositionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // ------------------------------------------------------------------ //
        // Public helpers                                                       //
        // ------------------------------------------------------------------ //

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false); // ordinary connection
        }

        // ------------------------------------------------------------------ //
        // Max Bill Cycle                                                       //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the max bill_cycle for the given area_code.
        /// SQL: select max(bill_cycle) from areas where area_code = ?
        /// </summary>
        public string GetMaxBillCycle(string areaCode)
        {
            if (string.IsNullOrWhiteSpace(areaCode))
                throw new ArgumentNullException(nameof(areaCode));

            logger.Info($"GetMaxBillCycle called for area_code={areaCode}");

            const string sql = "SELECT MAX(bill_cycle) FROM areas WHERE area_code = ?";

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", areaCode.Trim());

                        var result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                        {
                            logger.Warn($"No bill cycle found for area_code={areaCode}");
                            return null;
                        }

                        var billCycle = result.ToString().Trim();
                        logger.Info($"Max bill cycle for area_code={areaCode} is {billCycle}");
                        return billCycle;
                    }
                }
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, $"OleDb error in GetMaxBillCycle for area_code={areaCode}");
                throw new Exception($"Database error retrieving max bill cycle: {ex.Message}", ex);
            }
        }

        // ------------------------------------------------------------------ //
        // Main report                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the Areas Position report rows for the given request.
        /// If request.BillCycle is empty the max bill cycle is resolved first.
        /// </summary>
        public AreasPositionResult GetAreasPositionReport(AreasPositionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.AreaCode))
                throw new ArgumentException("AreaCode is required.", nameof(request));

            logger.Info($"=== START GetAreasPositionReport === area_code={request.AreaCode}, bill_cycle={request.BillCycle}");

            var rows = new List<AreasPositionModel>();

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Step 1 – resolve bill cycle if not supplied
                    string billCycle = string.IsNullOrWhiteSpace(request.BillCycle)
                        ? ResolveMaxBillCycle(conn, request.AreaCode)
                        : request.BillCycle.Trim();

                    if (string.IsNullOrEmpty(billCycle))
                    {
                        logger.Warn($"Could not resolve bill cycle for area_code={request.AreaCode}");
                        return new AreasPositionResult { BillCycle = null, Rows = rows };
                    }

                    logger.Info($"Using bill_cycle={billCycle}");

                    // Step 2 – fetch reader-level aggregates
                    var readerData = GetReaderAggregates(conn, billCycle, request.AreaCode);
                    logger.Info($"Retrieved {readerData.Count} reader records");

                    if (readerData.Count == 0)
                        return new AreasPositionResult { BillCycle = billCycle, Rows = rows };

                    // Step 3 – fetch NR transaction amounts per reader
                    var nrAmounts = GetNrTransactionAmounts(conn, billCycle, request.AreaCode, readerData);
                    logger.Info($"Retrieved NR transaction data for {nrAmounts.Count} readers");

                    // Step 4 – compute and format each row
                    foreach (var rd in readerData)
                    {
                        decimal kwhCharge = rd.KwhCharge;
                        decimal crntBalance = rd.CrntBalance;
                        int readerCount = rd.ReaderCount;
                        string readerCode = rd.ReaderCode;

                        // Subtract NR transactions (default 0 when not found / null)
                        nrAmounts.TryGetValue(readerCode, out decimal trnsAmount);

                        decimal charge = kwhCharge - trnsAmount;
                        decimal ratio = (charge != 0) ? crntBalance / charge : 0m;

                        rows.Add(new AreasPositionModel
                        {
                            ReaderCode = readerCode,
                            MonthlyBill = charge.ToString("###,###,###.#0"),
                            TotalBalance = crntBalance.ToString("###,###,###.#0"),
                            NoOfMonthsInArrears = ratio.ToString("##0.#0"),
                            NoOfAccounts = readerCount.ToString("###,###,##0"),
                            ErrorMessage = string.Empty
                        });
                    }

                    logger.Info($"=== END GetAreasPositionReport (Success) – {rows.Count} rows ===");

                    return new AreasPositionResult { BillCycle = billCycle, Rows = rows };
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred in GetAreasPositionReport");
                throw;
            }
        }

        // ------------------------------------------------------------------ //
        // Private helpers                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Resolves max(bill_cycle) for area_code using an already-open connection.
        /// </summary>
        private string ResolveMaxBillCycle(OleDbConnection conn, string areaCode)
        {
            const string sql = "SELECT MAX(bill_cycle) FROM areas WHERE area_code = ?";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", areaCode.Trim());
                var result = cmd.ExecuteScalar();
                return (result == null || result == DBNull.Value) ? null : result.ToString().Trim();
            }
        }

        /// <summary>
        /// Fetches per-reader aggregates from prn_dat_1.
        ///
        /// SQL (24-month): Select reader_code,
        ///                        sum((kwh_charge)+(fuel_charge)),
        ///                        sum(crnt_balance),
        ///                        count(reader_code)
        ///                 from prn_dat_1
        ///                 where bill_cycle=? and area_code=?
        ///                 group by reader_code
        ///                 order by reader_code
        /// </summary>
        private List<ReaderAggregate> GetReaderAggregates(OleDbConnection conn,
            string billCycle, string areaCode)
        {
            var result = new List<ReaderAggregate>();

            const string sql =
                "SELECT reader_code, " +
                "       SUM((kwh_charge) + (fuel_charge)), " +
                "       SUM(crnt_balance), " +
                "       COUNT(reader_code) " +
                "FROM   prn_dat_1 " +
                "WHERE  bill_cycle = ? " +
                "AND    area_code  = ? " +
                "GROUP  BY reader_code " +
                "ORDER  BY reader_code";

            try
            {
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", billCycle);
                    cmd.Parameters.AddWithValue("?", areaCode.Trim());

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new ReaderAggregate
                            {
                                ReaderCode = reader[0]?.ToString().Trim() ?? string.Empty,
                                KwhCharge = GetDecimalFromOrdinal(reader, 1),
                                CrntBalance = GetDecimalFromOrdinal(reader, 2),
                                ReaderCount = GetIntFromOrdinal(reader, 3)
                            });
                        }
                    }
                }
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "Error in GetReaderAggregates");
                throw new Exception("Database error retrieving reader aggregates: " + ex.Message, ex);
            }

            return result;
        }

        /// <summary>
        /// Fetches the sum of NR transaction amounts per reader_code.
        ///
        /// SQL: Select sum(t.transac_amt)
        ///      from prn_dat_1 p, prn_dat_2 t
        ///      where t.bill_cycle=? and p.area_code=? and p.reader_code=?
        ///        and t.transac_code like 'NR'
        ///        and p.area_code=t.area_code
        ///        and p.acct_number=t.acct_number
        ///
        /// Executed once per reader to match legacy VB row-by-row loop.
        /// Null / DBNull result is treated as 0 (matches legacy: If … Is Nothing Then TrnsAmount(i)=0).
        /// </summary>
        private Dictionary<string, decimal> GetNrTransactionAmounts(OleDbConnection conn,
            string billCycle, string areaCode, List<ReaderAggregate> readers)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            const string sql =
                "SELECT SUM(t.transac_amt) " +
                "FROM   prn_dat_1 p, prn_dat_2 t " +
                "WHERE  t.bill_cycle   = ? " +
                "AND    p.area_code    = ? " +
                "AND    p.reader_code  = ? " +
                "AND    t.transac_code LIKE 'NR' " +
                "AND    p.area_code    = t.area_code " +
                "AND    p.acct_number  = t.acct_number";

            try
            {
                foreach (var rd in readers)
                {
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", areaCode.Trim());
                        cmd.Parameters.AddWithValue("?", rd.ReaderCode);

                        var scalar = cmd.ExecuteScalar();

                        // Legacy: If result Is Nothing (DBNull) Then TrnsAmount = 0
                        decimal amount = (scalar == null || scalar == DBNull.Value)
                            ? 0m
                            : Convert.ToDecimal(scalar);

                        result[rd.ReaderCode] = amount;
                    }
                }
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "Error in GetNrTransactionAmounts");
                throw new Exception("Database error retrieving NR transaction amounts: " + ex.Message, ex);
            }

            return result;
        }

        // ------------------------------------------------------------------ //
        // Value helpers                                                        //
        // ------------------------------------------------------------------ //

        private decimal GetDecimalFromOrdinal(OleDbDataReader reader, int ordinal)
        {
            try
            {
                var val = reader[ordinal];
                return (val == null || val == DBNull.Value) ? 0m : Convert.ToDecimal(val);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not convert ordinal {ordinal} to decimal");
                return 0m;
            }
        }

        private int GetIntFromOrdinal(OleDbDataReader reader, int ordinal)
        {
            try
            {
                var val = reader[ordinal];
                return (val == null || val == DBNull.Value) ? 0 : Convert.ToInt32(val);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not convert ordinal {ordinal} to int");
                return 0;
            }
        }

        // ------------------------------------------------------------------ //
        // Private data classes                                                 //
        // ------------------------------------------------------------------ //

        private class ReaderAggregate
        {
            public string ReaderCode { get; set; }
            public decimal KwhCharge { get; set; }   // SUM(kwh_charge + fuel_charge)
            public decimal CrntBalance { get; set; }   // SUM(crnt_balance)
            public int ReaderCount { get; set; }   // COUNT(reader_code)
        }
    }
}