using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.SolarInformation
{
    public class SolarPaymentLissReconDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly (string NetType, string SchemeName)[] Schemes = new[]
        {
            ("1", "Net Metering"),
            ("2", "Net Accounting"),
            ("3", "Net Plus"),
            ("4", "Net Plus Plus"),
            ("5", "Convert Net Metering to Net Accounting")
        };

        public SolarPaymentLissReconResponse GetReconciliationReport(SolarPaymentLissReconRequest request)
        {
            var response = new SolarPaymentLissReconResponse
            {
                BillCycle = request.BillCycle,
                ReportCategory = request.ReportCategory,
                TypeCode = request.TypeCode
            };

            bool isDivision = string.Equals(request.ReportCategory, "Division", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(request.ReportCategory, "Region", StringComparison.OrdinalIgnoreCase);

            logger.Info($"=== START GetReconciliationReport === BillCycle={request.BillCycle}, Category={request.ReportCategory}, TypeCode={request.TypeCode}, IsDivision={isDivision}");

            // 1. Fetch Ordinary data (InformixConnection -> false)
            var ordFixed = GetOrdinaryFixed(request.BillCycle, request.TypeCode, isDivision);
            var ordOther = GetOrdinaryOther(request.BillCycle, request.TypeCode, isDivision);
            var ordVariable = GetOrdinaryVariable(request.BillCycle, request.TypeCode, isDivision);
            var ordFinancial = GetOrdinaryFinancial(request.BillCycle, request.TypeCode, isDivision);

            // 2. Fetch Bulk data (InformixBulkConnection -> true)
            var bulkFixed = GetBulkFixed(request.BillCycle, request.TypeCode, isDivision);
            var bulkOther = GetBulkOther(request.BillCycle, request.TypeCode, isDivision);
            var bulkVariable = GetBulkVariable(request.BillCycle, request.TypeCode, isDivision);
            var bulkFinancial = GetBulkFinancial(request.BillCycle, request.TypeCode, isDivision);

            var grandTotal = new SolarPaymentLissReconItem
            {
                NetType = "ALL",
                SchemeName = "Total"
            };

            foreach (var (netType, schemeName) in Schemes)
            {
                var row = new SolarPaymentLissReconItem
                {
                    NetType = netType,
                    SchemeName = schemeName,

                    // Ordinary
                    OrdFixedPayment = ordFixed.ContainsKey(netType) ? ordFixed[netType] : 0m,
                    OrdOtherAmount = ordOther.ContainsKey(netType) ? ordOther[netType] : 0m,
                    OrdVariablePayment = ordVariable.ContainsKey(netType) ? ordVariable[netType] : 0m,

                    // Bulk
                    BulkFixedPayment = bulkFixed.ContainsKey(netType) ? bulkFixed[netType] : 0m,
                    BulkOtherAmount = bulkOther.ContainsKey(netType) ? bulkOther[netType] : 0m,
                    BulkVariablePayment = bulkVariable.ContainsKey(netType) ? bulkVariable[netType] : 0m,
                };

                // Ordinary Total & Variance
                row.OrdTotalLiss = row.OrdFixedPayment + row.OrdOtherAmount + row.OrdVariablePayment;
                row.OrdFinancialReport = ordFinancial.ContainsKey(netType) ? ordFinancial[netType] : 0m;
                row.OrdDifference = row.OrdTotalLiss - row.OrdFinancialReport;

                // Bulk Total & Variance
                row.BulkTotalLiss = row.BulkFixedPayment + row.BulkOtherAmount + row.BulkVariablePayment;
                row.BulkFinancialReport = bulkFinancial.ContainsKey(netType) ? bulkFinancial[netType] : 0m;
                row.BulkDifference = row.BulkTotalLiss - row.BulkFinancialReport;

                // Combined Total
                row.TotalFixedPayment = row.OrdFixedPayment + row.BulkFixedPayment;
                row.TotalOtherAmount = row.OrdOtherAmount + row.BulkOtherAmount;
                row.TotalVariablePayment = row.OrdVariablePayment + row.BulkVariablePayment;
                row.TotalLiss = row.OrdTotalLiss + row.BulkTotalLiss;
                row.TotalFinancialReport = row.OrdFinancialReport + row.BulkFinancialReport;
                row.TotalDifference = row.TotalLiss - row.TotalFinancialReport;

                response.Rows.Add(row);

                // Add to Grand Total
                grandTotal.OrdFixedPayment += row.OrdFixedPayment;
                grandTotal.OrdOtherAmount += row.OrdOtherAmount;
                grandTotal.OrdVariablePayment += row.OrdVariablePayment;
                grandTotal.OrdTotalLiss += row.OrdTotalLiss;
                grandTotal.OrdFinancialReport += row.OrdFinancialReport;
                grandTotal.OrdDifference += row.OrdDifference;

                grandTotal.BulkFixedPayment += row.BulkFixedPayment;
                grandTotal.BulkOtherAmount += row.BulkOtherAmount;
                grandTotal.BulkVariablePayment += row.BulkVariablePayment;
                grandTotal.BulkTotalLiss += row.BulkTotalLiss;
                grandTotal.BulkFinancialReport += row.BulkFinancialReport;
                grandTotal.BulkDifference += row.BulkDifference;

                grandTotal.TotalFixedPayment += row.TotalFixedPayment;
                grandTotal.TotalOtherAmount += row.TotalOtherAmount;
                grandTotal.TotalVariablePayment += row.TotalVariablePayment;
                grandTotal.TotalLiss += row.TotalLiss;
                grandTotal.TotalFinancialReport += row.TotalFinancialReport;
                grandTotal.TotalDifference += row.TotalDifference;
            }

            response.GrandTotal = grandTotal;
            return response;
        }

        #region Ordinary Queries (billsmry@hqinfdb10)

        private Dictionary<string, decimal> GetOrdinaryFixed(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(kwh_sales) 
                           FROM netmtcons n, areas a 
                           WHERE n.calc_cycle = ? 
                             AND n.Rate IN ('15.50','22','34.50','37','23.18','27.06') 
                             AND (n.schm NOT IN ('3') OR n.schm IS NULL) 
                             AND a.area_code = n.area_code 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetOrdinaryFixed");
            }
            return dict;
        }

        private Dictionary<string, decimal> GetOrdinaryOther(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(kwh_sales) 
                           FROM netmtcons n, areas a 
                           WHERE n.calc_cycle = ? 
                             AND n.Rate NOT IN ('15.50','22','34.50','37','23.18','27.06') 
                             AND (n.schm NOT IN ('3') OR n.schm IS NULL) 
                             AND a.area_code = n.area_code 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetOrdinaryOther");
            }
            return dict;
        }

        private Dictionary<string, decimal> GetOrdinaryVariable(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(kwh_sales) 
                           FROM netmtcons n, areas a 
                           WHERE n.calc_cycle = ? 
                             AND n.schm = '3' 
                             AND a.area_code = n.area_code 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetOrdinaryVariable");
            }
            return dict;
        }

        private Dictionary<string, decimal> GetOrdinaryFinancial(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(n.kwh_sales) 
                           FROM netmtcons n, areas a 
                           WHERE n.calc_cycle = ? 
                             AND a.area_code = n.area_code 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetOrdinaryFinancial");
            }
            return dict;
        }

        #endregion

        #region Bulk Queries (billhsbhq@bulkinfdb1)

        private Dictionary<string, decimal> GetBulkFixed(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(kwh_sales) 
                           FROM netmtcons n, areas a, netmeter m 
                           WHERE n.bill_cycle = ? 
                             AND m.acc_nbr = n.acc_nbr 
                             AND m.schm IN ('1','2') 
                             AND rate IN ('22','15.50','34.50','37','27.06','23.18') 
                             AND a.area_code = n.area_cd 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetBulkFixed");
            }
            return dict;
        }

        private Dictionary<string, decimal> GetBulkOther(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(kwh_sales) 
                           FROM netmtcons n, areas a, netmeter m 
                           WHERE n.bill_cycle = ? 
                             AND m.acc_nbr = n.acc_nbr 
                             AND m.schm IN ('1','2') 
                             AND rate NOT IN ('22','15.50','34.50','37','27.06','23.18') 
                             AND a.area_code = n.area_cd 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetBulkOther");
            }
            return dict;
        }

        private Dictionary<string, decimal> GetBulkVariable(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(kwh_sales) 
                           FROM netmtcons n, netmeter m, areas a 
                           WHERE n.bill_cycle = ? 
                             AND m.schm = '3' 
                             AND m.acc_nbr = n.acc_nbr 
                             AND a.area_code = n.area_cd 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetBulkVariable");
            }
            return dict;
        }

        private Dictionary<string, decimal> GetBulkFinancial(string billCycle, string typeCode, bool isDivision)
        {
            var dict = new Dictionary<string, decimal>();
            string areaCol = isDivision ? "a.region" : "a.prov_code";
            string sql = $@"SELECT n.net_type, SUM(kwh_sales) 
                           FROM netmtcons n, areas a 
                           WHERE n.bill_cycle = ? 
                             AND n.area_cd = a.area_code 
                             AND {areaCol} = ? 
                           GROUP BY 1";

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        cmd.Parameters.AddWithValue("?", typeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader[0]?.ToString()?.Trim();
                                decimal val = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);
                                if (!string.IsNullOrEmpty(netType)) dict[netType] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error executing GetBulkFinancial");
            }
            return dict;
        }

        #endregion
    }
}
