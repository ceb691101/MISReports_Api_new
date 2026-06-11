using MISReports_Api.DBAccess;
using MISReports_Api.Models.CollectionInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.CollectionInformation
{
    public class ReceivePositionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true);
        }

        // Exposed only for the diagnostic endpoint in the controller
        public OleDbConnection GetRawConnection()
        {
            return _dbConnection.GetConnection(false);
        }

        // -----------------------------------------------------------------------
        // Dropdowns
        // -----------------------------------------------------------------------

        public ReceivePositionDropdowns GetDropdowns()
        {
            var dropdowns = new ReceivePositionDropdowns
            {
                BillCycles = new List<string>(),
                BillTypes = new List<string>(),
                Areas = new List<AreaOption>(),
                Provinces = new List<ProvinceOption>()   // NEW
            };

            try
            {
                logger.Info("=== START GetDropdowns ===");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Bill cycles
                    using (var cmd = new OleDbCommand(
                        "SELECT FIRST 12 DISTINCT bill_cycle FROM receive_position ORDER BY bill_cycle DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var val = GetColumnValue(reader, "bill_cycle");
                            if (!string.IsNullOrEmpty(val))
                                dropdowns.BillCycles.Add(val);
                        }
                    }

                    // Bill types
                    using (var cmd = new OleDbCommand(
                        "SELECT DISTINCT bill_type FROM receive_position ORDER BY bill_type", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var val = GetColumnValue(reader, "bill_type");
                            if (!string.IsNullOrEmpty(val))
                                dropdowns.BillTypes.Add(val);
                        }
                    }

                    // Areas
                    using (var cmd = new OleDbCommand(
                        "SELECT * FROM areas ORDER BY area_code", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var code = GetColumnValue(reader, "area_code");
                            if (!string.IsNullOrEmpty(code))
                            {
                                var name = GetColumnValue(reader, "area_name");
                                dropdowns.Areas.Add(new AreaOption
                                {
                                    AreaCode = code,
                                    AreaName = string.IsNullOrEmpty(name) ? code : name
                                });
                            }
                        }
                    }

                    // -----------------------------------------------------------
                    // NEW: Provinces  –  read from prov_servers
                    // Assumes columns: prov_code, prov_name
                    // Adjust column names below if your table differs.
                    // -----------------------------------------------------------
                    using (var cmd = new OleDbCommand(
                        "SELECT * FROM prov_servers ORDER BY prov_code", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var code = GetColumnValue(reader, "prov_code");
                            if (!string.IsNullOrEmpty(code))
                            {
                                var name = GetColumnValue(reader, "prov_name");
                                dropdowns.Provinces.Add(new ProvinceOption
                                {
                                    ProvCode = code,
                                    ProvName = string.IsNullOrEmpty(name) ? code : name
                                });
                            }
                        }
                    }
                }

                logger.Info(
                    $"=== END GetDropdowns: BillCycles={dropdowns.BillCycles.Count}, " +
                    $"BillTypes={dropdowns.BillTypes.Count}, " +
                    $"Areas={dropdowns.Areas.Count}, " +
                    $"Provinces={dropdowns.Provinces.Count} ===");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching dropdowns");
                throw;
            }

            return dropdowns;
        }

        // -----------------------------------------------------------------------
        // Main report  –  mirrors GetSolarReadingUsageBulkReport pattern exactly
        // -----------------------------------------------------------------------

        public List<ReceivePositionModel> GetReceivePositionReport(ReceivePositionRequest request)
        {
            var results = new List<ReceivePositionModel>();

            try
            {
                logger.Info("=== START GetReceivePositionReport ===");
                logger.Info($"Request: AreaCode={request.AreaCode}, BillCycle={request.BillCycle}, BillType={request.BillType}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Step 1: fetch receive_position rows for the given bill_type
                    var rows = GetReceivePositionRows(conn, request);
                    logger.Info($"Retrieved {rows.Count} rows from receive_position");

                    if (rows.Count == 0)
                    {
                        logger.Info("No data found");
                        return results;
                    }

                    // Step 2: enrich each row with the area name from areas table
                    foreach (var row in rows)
                    {
                        row.ErrorMessage = string.Empty;
                        results.Add(row);
                    }
                }

                logger.Info($"=== END GetReceivePositionReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching receive position report");
                throw;
            }
        }

        // -----------------------------------------------------------------------
        // Private: fetch rows
        //
        // When the caller passes a province code (e.g. "WP"), the WHERE clause
        // expands to all areas whose prov_code matches — via the sub-query on areas.
        // When an individual area code is passed the first predicate (rp.area_code = ?)
        // matches directly.
        //
        // bill_type is embedded as a literal (not a ? parameter) because the
        // Informix OleDb driver returns ISAM -111 when it is parameterized.
        // BillType is always "O" or "B" – validated by the controller.
        // -----------------------------------------------------------------------

        private List<ReceivePositionModel> GetReceivePositionRows(
            OleDbConnection conn, ReceivePositionRequest request)
        {
            var results = new List<ReceivePositionModel>();

            try
            {
                // Three area-filter modes:
                //   CEB      → no area filter  (return every area)
                //   Province → match area_code directly OR any area whose prov_code matches
                //   Area     → same query; first predicate matches the exact code
                bool isCebEntire = string.Equals(request.AreaCode, "CEB", StringComparison.OrdinalIgnoreCase);

                string sql;
                if (isCebEntire)
                {
                    sql = string.Format(
                        "SELECT rp.*, a.area_name FROM receive_position rp " +
                        "LEFT JOIN areas a ON rp.area_code = a.area_code " +
                        "WHERE rp.bill_cycle = ? AND rp.bill_type = '{0}'",
                        request.BillType);
                }
                else
                {
                    sql = string.Format(
                        "SELECT rp.*, a.area_name FROM receive_position rp " +
                        "LEFT JOIN areas a ON rp.area_code = a.area_code " +
                        "WHERE (rp.area_code = ? OR rp.area_code IN (SELECT area_code FROM areas WHERE prov_code = ?)) " +
                        "AND rp.bill_cycle = ? AND rp.bill_type = '{0}'",
                        request.BillType);
                }

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    if (isCebEntire)
                    {
                        cmd.Parameters.AddWithValue("?", request.BillCycle);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("?", request.AreaCode);
                        cmd.Parameters.AddWithValue("?", request.AreaCode);
                        cmd.Parameters.AddWithValue("?", request.BillCycle);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = new ReceivePositionModel
                            {
                                AreaCode = GetColumnValue(reader, "area_code"),
                                AreaName = GetColumnValue(reader, "area_name") ?? GetColumnValue(reader, "area_code"),
                                BillCycle = request.BillCycle,
                                BillType = request.BillType,
                                OpeningBalance = GetDecimalValue(reader, "op_bal"),
                                MonthlyCharge = GetDecimalValue(reader, "mon_chg"),
                                Debits = GetDecimalValue(reader, "debits"),
                                Credits = GetDecimalValue(reader, "credits"),
                                UnderCharge = GetDecimalValue(reader, "un_chg"),
                                OverCharge = GetDecimalValue(reader, "ov_chg"),
                                Payments = GetDecimalValue(reader, "payments"),
                                ClosingBalance = GetDecimalValue(reader, "close_bal"),
                                ClosingBalanceWithoutFinAcc = GetDecimalValue(reader, "cl_bal_fin"),
                                AverageCharge = GetDecimalValue(reader, "avg_3"),
                                NoOfMonthsInArrears = GetDecimalValue(reader, "no_months"),
                                NoOfMonthsInArrearsWithoutFinAcc = GetDecimalValue(reader, "mon_wofin")
                            };

                            results.Add(model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching receive_position rows");
                throw;
            }

            return results;
        }

        // -----------------------------------------------------------------------
        // Helper methods  –  identical to Solar DAO pattern
        // -----------------------------------------------------------------------

        private string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? null : value.ToString()?.Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return null;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{columnName}'");
                return 0;
            }
        }
    }
}