using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Web;

namespace MISReports_Api.DAL.General.ListOfGovernmentAccounts
{
    public class ListOfGovernmentAccountsDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Use bulk connection
        }

        /// <summary>
        /// Gets the maximum bill cycle for a specific area
        /// </summary>
        public MaxBillCycleModel GetMaxBillCycle(string areaCode)
        {
            var result = new MaxBillCycleModel();

            try
            {
                logger.Info($"Getting max bill cycle for area: {areaCode}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string sql = "SELECT max(bill_cycle) FROM prn_dat_1 WHERE area_code = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@area_code", areaCode);

                        var maxCycle = cmd.ExecuteScalar();
                        result.MaxBillCycle = maxCycle != null && maxCycle != DBNull.Value
                            ? maxCycle.ToString().Trim()
                            : string.Empty;

                        logger.Info($"Max bill cycle: {result.MaxBillCycle}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting max bill cycle");
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Gets all departments for dropdown
        /// </summary>
        public List<DepartmentModel> GetDepartments()
        {
            var departments = new List<DepartmentModel>();

            try
            {
                logger.Info("Getting departments list");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string sql = "SELECT dep_code, department FROM department ORDER BY dep_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var department = new DepartmentModel
                            {
                                DepartmentCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                                DepartmentName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim()
                            };

                            departments.Add(department);
                        }
                    }
                }

                logger.Info($"Retrieved {departments.Count} departments");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting departments");
                throw;
            }

            return departments;
        }

        /// <summary>
        /// Gets government accounts report based on request parameters
        /// </summary>
        public List<ListOfGovernmentAccountsModel> GetGovernmentAccountsReport(GovernmentAccountRequest request)
        {
            var results = new List<ListOfGovernmentAccountsModel>();

            try
            {
                logger.Info("=== START GetGovernmentAccountsReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, " +
                           $"AreaCode={request.AreaCode}, DepartmentCode={request.DepartmentCode}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    if (request.ReportType?.ToLower() == "area")
                    {
                        // Process Area report
                        results = GetAreaReportData(conn, request);
                    }
                    else if (request.ReportType?.ToLower() == "department")
                    {
                        // Process Department report
                        results = GetDepartmentReportData(conn, request);
                    }
                    else
                    {
                        logger.Warn($"Unsupported report type: {request.ReportType}");
                    }

                    logger.Info($"=== END GetGovernmentAccountsReport (Success) - {results.Count} records ===");
                    return results;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching government accounts report");
                throw;
            }
        }

        /// <summary>
        /// Gets data for Area report type
        /// </summary>
        private List<ListOfGovernmentAccountsModel> GetAreaReportData(OleDbConnection conn, GovernmentAccountRequest request)
        {
            var results = new List<ListOfGovernmentAccountsModel>();

            try
            {
                // SQL for Area report - includes all government accounts for the area
                string sql = @"SELECT a.acct_number, a.cust_fname, a.cust_lname, 
                                      a.address_1, a.address_2, a.address_3, 
                                      a.crnt_balance, a.kwh_charge, a.avg_cons 
                               FROM prn_dat_1 a 
                               INNER JOIN govt_acct b ON a.acct_number = b.acct_number
                               WHERE a.area_code = ? 
                                 AND a.bill_cycle = ? 
                               ORDER BY a.acct_number";

                logger.Info($"Executing Area SQL with BillCycle={request.BillCycle}, AreaCode={request.AreaCode}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@area_code", request.AreaCode);
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);
                            model.AreaCode = request.AreaCode;
                            model.BillCycle = request.BillCycle;

                            // Get area name from areas table
                            model.AreaName = GetAreaName(conn, request.AreaCode);

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
        /// Gets data for Department report type
        /// </summary>
        private List<ListOfGovernmentAccountsModel> GetDepartmentReportData(OleDbConnection conn, GovernmentAccountRequest request)
        {
            var results = new List<ListOfGovernmentAccountsModel>();

            try
            {
                // SQL for Department report - includes government accounts filtered by department
                string sql = @"SELECT a.acct_number, a.cust_fname, a.cust_lname, 
                                      a.address_1, a.address_2, a.address_3, 
                                      a.crnt_balance, a.kwh_charge, a.avg_cons 
                               FROM prn_dat_1 a 
                               INNER JOIN govt_acct b ON a.acct_number = b.acct_number
                               WHERE a.area_code = ? 
                                 AND a.bill_cycle = ? 
                                 AND b.dept = ? 
                               ORDER BY a.acct_number";

                logger.Info($"Executing Department SQL with BillCycle={request.BillCycle}, " +
                           $"AreaCode={request.AreaCode}, DepartmentCode={request.DepartmentCode}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@area_code", request.AreaCode);
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@dept", request.DepartmentCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);
                            model.AreaCode = request.AreaCode;
                            model.BillCycle = request.BillCycle;
                            model.DepartmentCode = request.DepartmentCode;

                            // Get area name from areas table
                            model.AreaName = GetAreaName(conn, request.AreaCode);

                            // Get department name
                            model.DepartmentName = GetDepartmentName(conn, request.DepartmentCode);

                            results.Add(model);
                        }
                    }
                }

                logger.Info($"Retrieved {results.Count} records for Department report");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Department report data");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Gets area name from area code
        /// </summary>
        private string GetAreaName(OleDbConnection conn, string areaCode)
        {
            try
            {
                string sql = "SELECT area_name FROM areas WHERE area_code = ?";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@area_code", areaCode);
                    var result = cmd.ExecuteScalar();

                    return result != null && result != DBNull.Value
                        ? result.ToString().Trim()
                        : string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error getting area name for code: {areaCode}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets department name from department code
        /// </summary>
        private string GetDepartmentName(OleDbConnection conn, string departmentCode)
        {
            try
            {
                string sql = "SELECT department FROM department WHERE dep_code = ?";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@dep_code", departmentCode);
                    var result = cmd.ExecuteScalar();

                    return result != null && result != DBNull.Value
                        ? result.ToString().Trim()
                        : string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error getting department name for code: {departmentCode}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Maps database reader to model
        /// </summary>
        private ListOfGovernmentAccountsModel MapReaderToModel(OleDbDataReader reader)
        {
            var model = new ListOfGovernmentAccountsModel();

            try
            {
                // Get raw values using ordinal positions
                model.AccountNumber = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();
                model.CustomerFirstName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                model.CustomerLastName = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();

                // Combine first and last name
                model.CustomerName = $"{model.CustomerFirstName} {model.CustomerLastName}".Trim();

                // Get address lines
                model.Address1 = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
                model.Address2 = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim();
                model.Address3 = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim();

                // Combine address lines
                var addressParts = new List<string>();
                if (!string.IsNullOrEmpty(model.Address1)) addressParts.Add(model.Address1);
                if (!string.IsNullOrEmpty(model.Address2)) addressParts.Add(model.Address2);
                if (!string.IsNullOrEmpty(model.Address3)) addressParts.Add(model.Address3);
                model.Address = string.Join(" ", addressParts);

                // Get raw numeric values
                model.RawCurrentBalance = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6));
                model.RawKwhCharge = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7));
                model.RawAverageConsumption = reader.IsDBNull(8) ? 0 : Convert.ToDecimal(reader.GetValue(8));

                // Format for display
                model.CurrentBalance = FormatDecimal(model.RawCurrentBalance);
                model.KwhCharge = FormatDecimal(model.RawKwhCharge);
                model.AverageConsumption = FormatDecimal(model.RawAverageConsumption);

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
        /// Formats decimal value with commas and 2 decimal places
        /// </summary>
        private string FormatDecimal(decimal value)
        {
            try
            {
                return value.ToString("###,###.##");
            }
            catch
            {
                return "0.00";
            }
        }
    }
}