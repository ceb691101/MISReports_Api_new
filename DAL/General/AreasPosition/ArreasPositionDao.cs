using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.General.ArrearsPosition
{
    /// <summary>
    /// Retrieves meter-reader-wise arrears position data from the
    /// <c>billsmry</c> (bulk) database tables <c>prn_dat_1</c> and <c>prn_dat_2</c>.
    ///
    /// Business logic (ported from legacy VB):
    ///   charge(i)  = sum(kwh_charge + fuel_charge) – sum(NR transac_amt)
    ///   ratio(i)   = crnt_balance / charge   (0 when charge = 0)
    /// </summary>
    public class ArrearsPositionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // bulk connection
        }

        // ------------------------------------------------------------------ //
        //  Public entry point                                                  //
        // ------------------------------------------------------------------ //

        public List<ArrearsPositionModel> GetArrearsPositionReport(ArrearsPositionRequest request)
        {
            var results = new List<ArrearsPositionModel>();

            try
            {
                logger.Info("=== START GetArrearsPositionReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, AreaCode={request.AreaCode}");

                using (var conn = _dbConnection.GetConnection(false)) // billsmry bulk connection
                {
                    // FIX: only open if not already open
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    results = FetchReaderRows(conn, request);
                }

                logger.Info($"=== END GetArrearsPositionReport (Success) - {results.Count} records ===");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching arrears position report");
                throw;
            }

            return results;
        }

        // ------------------------------------------------------------------ //
        //  Step 1 – pull one row per reader_code                              //
        // ------------------------------------------------------------------ //

        private List<ArrearsPositionModel> FetchReaderRows(OleDbConnection conn, ArrearsPositionRequest request)
        {
            var results = new List<ArrearsPositionModel>();

            const string sql = @"SELECT reader_code,
                                        SUM(kwh_charge + fuel_charge),
                                        SUM(crnt_balance),
                                        COUNT(reader_code)
                                 FROM   prn_dat_1
                                 WHERE  bill_cycle = ?
                                   AND  area_code  = ?
                                 GROUP  BY reader_code
                                 ORDER  BY reader_code";

            logger.Info($"Executing reader SQL: BillCycle={request.BillCycle}, AreaCode={request.AreaCode}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("p1", request.BillCycle);
                cmd.Parameters.AddWithValue("p2", request.AreaCode);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string readerCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();
                        decimal kwhCharge = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1));
                        decimal crntBalance = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2));
                        int readerCount = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3));

                        decimal nrAmount = FetchNRTransactionAmount(conn, request.BillCycle, request.AreaCode, readerCode);

                        decimal charge = kwhCharge - nrAmount;
                        decimal ratio = charge != 0m ? crntBalance / charge : 0m;

                        results.Add(new ArrearsPositionModel
                        {
                            ReaderCode = readerCode,
                            RawCharge = charge,
                            RawCurrentBalance = crntBalance,
                            RawRatio = ratio,
                            RawReaderCount = readerCount,
                            Charge = FormatAmount(charge),
                            CurrentBalance = FormatAmount(crntBalance),
                            Ratio = FormatRatio(ratio),
                            ReaderCount = FormatCount(readerCount),
                            ErrorMessage = string.Empty
                        });
                    }
                }
            }

            logger.Info($"Retrieved {results.Count} reader rows");
            return results;
        }

        // ------------------------------------------------------------------ //
        //  Step 2 – NR transaction sum for a single reader                    //
        // ------------------------------------------------------------------ //

        private decimal FetchNRTransactionAmount(
            OleDbConnection conn,
            string billCycle,
            string areaCode,
            string readerCode)
        {
            const string sql = @"SELECT SUM(t.transac_amt)
                                 FROM   prn_dat_1 p,
                                        prn_dat_2 t
                                 WHERE  t.bill_cycle   = ?
                                   AND  p.area_code    = ?
                                   AND  p.reader_code  = ?
                                   AND  t.transac_code LIKE 'NR'
                                   AND  p.area_code    = t.area_code
                                   AND  p.acct_number  = t.acct_number";

            try
            {
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("p1", billCycle);
                    cmd.Parameters.AddWithValue("p2", areaCode);
                    cmd.Parameters.AddWithValue("p3", readerCode);

                    object result = cmd.ExecuteScalar();
                    return (result == null || result == DBNull.Value) ? 0m : Convert.ToDecimal(result);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error fetching NR transaction amount for reader={readerCode}");
                return 0m;
            }
        }

        // ------------------------------------------------------------------ //
        //  Formatting helpers (match legacy VB format strings)                //
        // ------------------------------------------------------------------ //

        private string FormatAmount(decimal value)
        {
            try { return value.ToString("###,###,###.#0"); }
            catch { return "0.00"; }
        }

        private string FormatRatio(decimal value)
        {
            try { return value.ToString("##0.#0"); }
            catch { return "0.00"; }
        }

        private string FormatCount(int value)
        {
            try { return value.ToString("###,###,##0"); }
            catch { return "0"; }
        }
    }
}