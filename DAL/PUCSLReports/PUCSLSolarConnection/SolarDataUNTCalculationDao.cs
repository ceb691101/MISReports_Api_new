using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.PUCSLReports;
using MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.PUCSLReports.PUCSLSolarConnection
{
    public class SolarDataUNTCalculationDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestAllConnections(out errorMessage);
        }

        // ================================================================
        //  PUBLIC ENTRY POINT
        // ================================================================
        public SolarDataUNTCalculationResponse GetSolarDataUNTReport(PUCSLRequest request)
        {
            var response = new SolarDataUNTCalculationResponse
            {
                Data = new List<SolarDataUNTCalculationModel>()
            };

            try
            {
                logger.Info("=== START GetSolarDataUNTReport ===");
                logger.Info($"Category={request.ReportCategory}, TypeCode={request.TypeCode}, BillCycle={request.BillCycle}");

                SolarReportType reportType = MapReportType(request.ReportCategory);

                // Get Year and Month from BillCycle
                var (year, month) = GetYearMonthFromCycle(request.BillCycle);

                // Get all tariff categories in order (from tariff_category table)
                var categories = GetTariffCategories();
                if (categories.Count == 0)
                {
                    logger.Warn("No tariff categories found.");
                    response.ErrorMessage = "No tariff categories found in database.";
                    return response;
                }

                // Process each category
                foreach (var category in categories)
                {
                    var rowModel = new SolarDataUNTCalculationModel
                    {
                        Category = category,
                        Year = year,
                        Month = month,
                        Accts = 0,
                        UnitsExpD = 0,
                        UnitsExpP = 0,
                        UnitsExpOffP = 0,
                        UnitsImpD = 0,
                        UnitsImpP = 0,
                        UnitsImpOffP = 0
                    };

                    int totAccts = 0;
                    decimal unitsExpDay = 0;
                    decimal unitsImpDay = 0;

                    int totAcctsBulk = 0;
                    decimal unitsExpDayBulk = 0;
                    decimal unitsImpDayBulk = 0;
                    decimal unitsImpPeakBulk = 0;
                    decimal unitsImpOffPeakBulk = 0;

                    // 1) Get Ordinary tariff codes for this category
                    var ordinaryTariffs = GetTariffCodesForCategory(category, "O");
                    foreach (var tariffCode in ordinaryTariffs)
                    {
                        var ordData = GetOrdinaryData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                        totAccts += ordData.customers;
                        unitsExpDay += ordData.unitsExp;
                        unitsImpDay += ordData.unitsImp;
                    }

                    // 2) Get Bulk tariff codes for this category
                    var bulkTariffs = GetTariffCodesForCategory(category, "B");
                    foreach (var tariffCode in bulkTariffs)
                    {
                        var bulkData = GetBulkData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                        totAcctsBulk += bulkData.customers;
                        unitsExpDayBulk += bulkData.unitsExp;
                        unitsImpDayBulk += bulkData.unitsImp;
                        unitsImpPeakBulk += bulkData.unitsImpPeak;
                        unitsImpOffPeakBulk += bulkData.unitsImpOffPeak;
                    }

                    // 3) Aggregate values
                    rowModel.Accts = totAccts + totAcctsBulk;
                    rowModel.UnitsExpD = unitsExpDay + unitsExpDayBulk;
                    rowModel.UnitsExpP = 0;
                    rowModel.UnitsExpOffP = 0;
                    rowModel.UnitsImpD = unitsImpDay + unitsImpDayBulk;
                    rowModel.UnitsImpP = unitsImpPeakBulk;
                    rowModel.UnitsImpOffP = unitsImpOffPeakBulk;

                    response.Data.Add(rowModel);
                }

                // 4) Calculate Total
                var totalRow = new SolarDataUNTCalculationModel
                {
                    Category = "Total",
                    Year = year,
                    Month = month,
                    Accts = 0,
                    UnitsExpD = 0,
                    UnitsExpP = 0,
                    UnitsExpOffP = 0,
                    UnitsImpD = 0,
                    UnitsImpP = 0,
                    UnitsImpOffP = 0
                };

                foreach (var row in response.Data)
                {
                    totalRow.Accts += row.Accts;
                    totalRow.UnitsExpD += row.UnitsExpD;
                    totalRow.UnitsExpP += row.UnitsExpP;
                    totalRow.UnitsExpOffP += row.UnitsExpOffP;
                    totalRow.UnitsImpD += row.UnitsImpD;
                    totalRow.UnitsImpP += row.UnitsImpP;
                    totalRow.UnitsImpOffP += row.UnitsImpOffP;
                }

                response.Total = totalRow;
                response.ErrorMessage = string.Empty;
                logger.Info($"SolarDataUNTReport completed. {response.Data.Count} categories.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSolarDataUNTReport EXCEPTION");
                response.ErrorMessage = $"Error: {ex.Message}";
            }

            return response;
        }

        // ================================================================
        //  GET TARIFF CATEGORIES
        // ================================================================
        private List<string> GetTariffCategories()
        {
            var categories = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string sql = "SELECT tariff_cat FROM tariff_category ORDER BY seq";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var category = reader[0]?.ToString().Trim();
                            if (!string.IsNullOrEmpty(category))
                            {
                                categories.Add(category);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetTariffCategories EXCEPTION");
            }
            return categories;
        }

        // ================================================================
        //  GET TARIFF CODES FOR CATEGORY
        // ================================================================
        private List<string> GetTariffCodesForCategory(string category, string cusCategory)
        {
            var tariffCodes = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string sql = "SELECT c.tariff_code FROM cat_tariff_table c, tariff_category t " +
                                 "WHERE c.tariff_cat=t.tariff_cat AND t.tariff_cat=? AND cus_cat=?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", category);
                        cmd.Parameters.AddWithValue("?", cusCategory);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tariffCode = reader[0]?.ToString().Trim();
                                if (!string.IsNullOrEmpty(tariffCode))
                                {
                                    tariffCodes.Add(tariffCode);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetTariffCodesForCategory cat={category}, cusCat={cusCategory}");
            }
            return tariffCodes;
        }

        // ================================================================
        //  GET ORDINARY DATA
        // ================================================================
        private (int customers, decimal unitsExp, decimal unitsImp) GetOrdinaryData(SolarReportType rt, string typeCode,
            string calcCycle, string tariffCode)
        {
            int customers = 0;
            decimal unitsExp = 0;
            decimal unitsImp = 0;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = "SELECT COUNT(n.acct_number), COALESCE(SUM(n.units_out),0), COALESCE(SUM(n.units_in),0) " +
                                  "FROM netmtcons n, areas a " +
                                  "WHERE n.net_type='1' AND n.calc_cycle =? AND a.area_code=n.area_code AND a.prov_code=? AND tariff_code=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            break;

                        case SolarReportType.Region:
                            sql = "SELECT COUNT(n.acct_number), COALESCE(SUM(n.units_out),0), COALESCE(SUM(n.units_in),0) " +
                                  "FROM netmtcons n, areas a " +
                                  "WHERE n.net_type='1' AND n.calc_cycle =? AND a.area_code=n.area_code AND a.region=? AND tariff_code=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            break;

                        default: // EntireCEB
                            sql = "SELECT COUNT(acct_number), COALESCE(SUM(units_out),0), COALESCE(SUM(units_in),0) " +
                                  "FROM netmtcons " +
                                  "WHERE net_type='1' AND calc_cycle =? AND tariff_code=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            unitsExp = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]); // units_out
                            unitsImp = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]); // units_in
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdinaryData tariffCode={tariffCode}");
            }

            return (customers, unitsExp, unitsImp);
        }

        // ================================================================
        //  GET BULK DATA
        // ================================================================
        private (int customers, decimal unitsExp, decimal unitsImp, decimal unitsImpPeak, decimal unitsImpOffPeak) GetBulkData(
            SolarReportType rt, string typeCode, string billCycle, string tariff)
        {
            int customers = 0;
            decimal unitsExp = 0;
            decimal unitsImp = 0;
            decimal unitsImpPeak = 0;
            decimal unitsImpOffPeak = 0;

            try
            {
                string bulkTypeCode = (rt == SolarReportType.Province)
                    ? typeCode.PadLeft(2, '0')  // "3" → "03"
                    : typeCode;

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = "SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0), COALESCE(SUM(n.imp_kwd_units),0), " +
                                  "COALESCE(SUM(n.imp_kwp_units),0), COALESCE(SUM(n.imp_kwo_units),0) " +
                                  "FROM netmtcons n, areas a " +
                                  "WHERE bill_cycle=? AND n.net_type='1' AND a.area_code=n.area_cd AND a.prov_code=? AND tariff=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                            break;

                        case SolarReportType.Region:
                            sql = "SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0), COALESCE(SUM(n.imp_kwd_units),0), " +
                                  "COALESCE(SUM(n.imp_kwp_units),0), COALESCE(SUM(n.imp_kwo_units),0) " +
                                  "FROM netmtcons n, areas a " +
                                  "WHERE bill_cycle=? AND n.net_type='1' AND a.area_code=n.area_cd AND a.region=? AND tariff=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                            break;

                        default: // EntireCEB
                            sql = "SELECT COUNT(acc_nbr), COALESCE(SUM(exp_kwd_units),0), COALESCE(SUM(imp_kwd_units),0), " +
                                  "COALESCE(SUM(imp_kwp_units),0), COALESCE(SUM(imp_kwo_units),0) " +
                                  "FROM netmtcons " +
                                  "WHERE bill_cycle=? AND net_type='1' AND tariff=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            unitsExp = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);        // exp_kwd_units
                            unitsImp = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);        // imp_kwd_units
                            unitsImpPeak = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]);    // imp_kwp_units
                            unitsImpOffPeak = reader[4] == DBNull.Value ? 0 : Convert.ToDecimal(reader[4]); // imp_kwo_units
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkData tariff={tariff}");
            }

            return (customers, unitsExp, unitsImp, unitsImpPeak, unitsImpOffPeak);
        }

        // ================================================================
        //  UTILITY
        // ================================================================
        private SolarReportType MapReportType(PUCSLReportCategory cat)
        {
            switch (cat)
            {
                case PUCSLReportCategory.Province: return SolarReportType.Province;
                case PUCSLReportCategory.Region: return SolarReportType.Region;
                default: return SolarReportType.EntireCEB;
            }
        }

        private (string year, string month) GetYearMonthFromCycle(string billCycle)
        {
            try
            {
                int cycle = int.Parse(billCycle);
                string monthYear = BillCycleHelper.ConvertToMonthYear(cycle);

                if (string.IsNullOrEmpty(monthYear) || monthYear == "Invalid" || monthYear == "Unknown")
                {
                    logger.Warn($"Invalid bill cycle: {billCycle}");
                    return ("", "");
                }

                var parts = monthYear.Split(' ');
                if (parts.Length == 2)
                {
                    string monthName = parts[0];
                    string year = parts[1];
                    string monthNumber = ConvertMonthNameToNumber(monthName);
                    return (year, monthNumber);
                }

                return ("", "");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error parsing bill cycle {billCycle}");
                return ("", "");
            }
        }

        private string ConvertMonthNameToNumber(string monthName)
        {
            switch (monthName)
            {
                case "Jan": return "1";
                case "Feb": return "2";
                case "Mar": return "3";
                case "Apr": return "4";
                case "May": return "5";
                case "Jun": return "6";
                case "Jul": return "7";
                case "Aug": return "8";
                case "Sep": return "9";
                case "Oct": return "10";
                case "Nov": return "11";
                case "Dec": return "12";
                default: return monthName;
            }
        }
    }
}
