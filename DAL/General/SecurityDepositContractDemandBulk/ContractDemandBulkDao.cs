using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Web;

namespace MISReports_Api.DAL.General.SecurityDepositContractDemandBulk
{
    public class ContractDemandBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Use bulk connection
        }

        public List<SecDepositConDemandBulkModel> GetContractDemandBulkReport(SecDepositConDemandRequest request)
        {
            var results = new List<SecDepositConDemandBulkModel>();

            try
            {
                logger.Info("=== START GetContractDemandBulkReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, " +
                           $"AreaCode={request.AreaCode}, ProvCode={request.ProvCode}");

                using (var conn = _dbConnection.GetConnection(true)) // Use bulk connection
                {
                    conn.Open();

                    if (request.ReportType == SolarReportType.Area)
                    {
                        // Process Area report
                        results = GetAreaReportData(conn, request);
                    }
                    else if (request.ReportType == SolarReportType.Province)
                    {
                        // Process Province report
                        results = GetProvinceReportData(conn, request);
                    }
                    else
                    {
                        // Handle other report types if needed
                        logger.Warn($"Unsupported report type for this DAO: {request.ReportType}");
                    }

                    logger.Info($"=== END GetContractDemandBulkReport (Success) - {results.Count} records ===");
                    return results;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching contract demand bulk report");
                throw;
            }
        }

        /// <summary>
        /// Gets data for Area report type
        /// </summary>
        private List<SecDepositConDemandBulkModel> GetAreaReportData(OleDbConnection conn, SecDepositConDemandRequest request)
        {
            var results = new List<SecDepositConDemandBulkModel>();

            try
            {
                string sql = @"SELECT c.acc_nbr, c.name, c.address_l1, c.address_l2, c.city, c.tariff, 
                                      c.cntr_dmnd, c.tot_sec_dep, m.tot_untskwo, m.tot_untskwd, 
                                      m.tot_untskwp, m.tot_kva, m.tot_amt 
                               FROM customer c, mon_tot m 
                               WHERE m.bill_cycle = ? 
                                 AND c.cst_st = '0' 
                                 AND c.acc_nbr = m.acc_nbr 
                                 AND c.area_cd = ? 
                               ORDER BY c.acc_nbr";

                logger.Info($"Executing Area SQL with BillCycle={request.BillCycle}, AreaCode={request.AreaCode}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@area_cd", request.AreaCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);
                            model.AreaCode = request.AreaCode;
                            model.BillCycle = request.BillCycle;
                            results.Add(model);
                        }
                    }
                }

                logger.Info($"Retrieved {results.Count} records for Area report");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Area report data");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Gets data for Province report type with area and province information
        /// </summary>
        private List<SecDepositConDemandBulkModel> GetProvinceReportData(OleDbConnection conn, SecDepositConDemandRequest request)
        {
            var results = new List<SecDepositConDemandBulkModel>();

            try
            {
                // First, get the main data with area and province codes
                string sql = @"SELECT c.acc_nbr, c.name, c.address_l1, c.address_l2, c.city, c.tariff, 
                                      c.cntr_dmnd, c.tot_sec_dep, m.tot_untskwo, m.tot_untskwd, 
                                      m.tot_untskwp, m.tot_kva, m.tot_amt, a.area_code, p.prov_code
                               FROM customer c, mon_tot m, areas a, provinces p 
                               WHERE m.bill_cycle = ? 
                                 AND c.cst_st = '0' 
                                 AND c.acc_nbr = m.acc_nbr 
                                 AND c.area_cd = a.area_code 
                                 AND a.prov_code = p.prov_code 
                                 AND p.prov_code = ? 
                               ORDER BY c.acc_nbr, a.area_code";

                logger.Info($"Executing Province SQL with BillCycle={request.BillCycle}, ProvCode={request.ProvCode}");

                var accountAreaMap = new Dictionary<string, ProvinceAreaInfo>();

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@prov_code", request.ProvCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);

                            // Get area and province codes
                            var areaCode = GetColumnValue(reader, "area_code");
                            var provCode = GetColumnValue(reader, "prov_code");

                            model.AreaCode = areaCode;
                            model.ProvinceCode = provCode;
                            model.BillCycle = request.BillCycle;

                            // Store area info for later enrichment
                            if (!string.IsNullOrEmpty(areaCode))
                            {
                                accountAreaMap[model.AccountNumber] = new ProvinceAreaInfo
                                {
                                    AccountNumber = model.AccountNumber,
                                    AreaCode = areaCode,
                                    ProvinceCode = provCode
                                };
                            }

                            results.Add(model);
                        }
                    }
                }

                // Enrich with area and province names if we have data
                if (results.Count > 0)
                {
                    EnrichWithLocationNames(conn, results, accountAreaMap);
                }

                logger.Info($"Retrieved {results.Count} records for Province report");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Province report data");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Maps database reader to model
        /// </summary>
        private SecDepositConDemandBulkModel MapReaderToModel(OleDbDataReader reader)
        {
            var model = new SecDepositConDemandBulkModel();

            try
            {
                // Get raw values
                model.AccountNumber = GetColumnValue(reader, "acc_nbr") ?? "";
                model.Name = GetColumnValue(reader, "name")?.Trim() ?? "";

                // Combine address lines
                string addr1 = GetColumnValue(reader, "address_l1")?.Trim() ?? "";
                string addr2 = GetColumnValue(reader, "address_l2")?.Trim() ?? "";
                model.Address = string.IsNullOrEmpty(addr2) ? addr1 : $"{addr1} {addr2}";

                model.City = GetColumnValue(reader, "city")?.Trim() ?? "";
                model.Tariff = GetColumnValue(reader, "tariff") ?? "";

                // Get raw numeric values
                model.RawContractDemand = GetDecimalValue(reader, "cntr_dmnd");
                model.RawSecurityDeposit = GetDecimalValue(reader, "tot_sec_dep");
                model.RawTotalKWOUnits = GetDecimalValue(reader, "tot_untskwo");
                model.RawTotalKWDUnits = GetDecimalValue(reader, "tot_untskwd");
                model.RawTotalKWPUnits = GetDecimalValue(reader, "tot_untskwp");
                model.RawKVA = GetDecimalValue(reader, "tot_kva");
                model.RawMonthlyCharge = GetDecimalValue(reader, "tot_amt");

                // Format for display (as per VB6 code)
                model.ContractDemand = FormatInteger(model.RawContractDemand);
                model.SecurityDeposit = FormatDecimal(model.RawSecurityDeposit);
                model.TotalKWOUnits = FormatInteger(model.RawTotalKWOUnits);
                model.TotalKWDUnits = FormatInteger(model.RawTotalKWDUnits);
                model.TotalKWPUnits = FormatInteger(model.RawTotalKWPUnits);
                model.KVA = FormatInteger(model.RawKVA);
                model.MonthlyCharge = FormatDecimal(model.RawMonthlyCharge);

                model.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping reader to model");
                model.ErrorMessage = ex.Message;
            }

            return model;
        }

        /// <summary>
        /// Enriches province report results with area and province names
        /// </summary>
        private void EnrichWithLocationNames(OleDbConnection conn,
            List<SecDepositConDemandBulkModel> results,
            Dictionary<string, ProvinceAreaInfo> accountAreaMap)
        {
            try
            {
                // Get unique area codes
                var areaCodes = accountAreaMap.Values
                    .Select(v => v.AreaCode)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList();

                if (areaCodes.Count == 0)
                    return;

                // Get area names
                var areaNames = GetAreaNamesBatch(conn, areaCodes);

                // Get unique province codes
                var provCodes = accountAreaMap.Values
                    .Select(v => v.ProvinceCode)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList();

                // Get province names
                var provinceNames = GetProvinceNamesBatch(conn, provCodes);

                // Enrich each result
                foreach (var result in results)
                {
                    if (accountAreaMap.TryGetValue(result.AccountNumber, out var info))
                    {
                        if (!string.IsNullOrEmpty(info.AreaCode) && areaNames.TryGetValue(info.AreaCode, out string areaName))
                        {
                            result.Area = areaName;
                        }

                        if (!string.IsNullOrEmpty(info.ProvinceCode) && provinceNames.TryGetValue(info.ProvinceCode, out string provName))
                        {
                            result.Province = provName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error enriching with location names");
            }
        }

        /// <summary>
        /// Gets area names for multiple area codes
        /// </summary>
        private Dictionary<string, string> GetAreaNamesBatch(OleDbConnection conn, List<string> areaCodes)
        {
            var result = new Dictionary<string, string>();

            try
            {
                var parameters = string.Join(",", areaCodes.Select((_, idx) => $"?"));

                string sql = $@"SELECT area_code, area_name 
                               FROM areas 
                               WHERE area_code IN ({parameters})";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    foreach (var areaCode in areaCodes)
                    {
                        cmd.Parameters.AddWithValue("?", areaCode);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var code = GetColumnValue(reader, "area_code");
                            var name = GetColumnValue(reader, "area_name")?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(code))
                            {
                                result[code] = name;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching area names batch");
            }

            return result;
        }

        /// <summary>
        /// Gets province names for multiple province codes
        /// </summary>
        private Dictionary<string, string> GetProvinceNamesBatch(OleDbConnection conn, List<string> provCodes)
        {
            var result = new Dictionary<string, string>();

            try
            {
                var parameters = string.Join(",", provCodes.Select((_, idx) => $"?"));

                string sql = $@"SELECT prov_code, prov_name 
                               FROM provinces 
                               WHERE prov_code IN ({parameters})";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    foreach (var provCode in provCodes)
                    {
                        cmd.Parameters.AddWithValue("?", provCode);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var code = GetColumnValue(reader, "prov_code");
                            var name = GetColumnValue(reader, "prov_name")?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(code))
                            {
                                result[code] = name;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching province names batch");
            }

            return result;
        }

        // Helper methods
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

        private string FormatInteger(decimal value)
        {
            try
            {
                int intValue = (int)value;
                return intValue.ToString("###,###");
            }
            catch
            {
                return "0";
            }
        }

        private string FormatDecimal(decimal value)
        {
            try
            {
                // Format with commas and 2 decimal places as per VB6 code: "###,###.#0"
                return value.ToString("###,###.##");
            }
            catch
            {
                return "0.00";
            }
        }
    }
}