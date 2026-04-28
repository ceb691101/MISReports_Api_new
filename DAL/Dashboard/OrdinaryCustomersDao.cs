using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Dashboard
{
    public class OrdinaryCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false); // Use ordinary connection
        }

        public OrdinaryCustomers GetOrdinaryCustomersCount(string region = null)
        {
            var result = new OrdinaryCustomers { TotalCount = 0, BillCycle = "" };

            try
            {
                logger.Info("=== START GetOrdinaryCustomersCount ===");

                using (var conn = _dbConnection.GetConnection(false)) // false = ordinary connection
                {
                    conn.Open();

                    string maxBillCycleSql = "select max(bill_cycle) from areas";
                    int maxBillCycle;

                    using (var maxCmd = new OleDbCommand(maxBillCycleSql, conn))
                    {
                        var maxCycleValue = maxCmd.ExecuteScalar();
                        if (maxCycleValue == null || maxCycleValue == DBNull.Value)
                        {
                            return result;
                        }

                        if (!int.TryParse(maxCycleValue.ToString(), out maxBillCycle))
                        {
                            return result;
                        }
                    }

                    result.BillCycle = (maxBillCycle - 2).ToString();

                    string sql = @"select sum(c.cnt)
                                from consmry c, areas a
                                where c.area_code = a.area_code
                                and c.bill_cycle = (select max(bill_cycle) from areas) - 2";

                    if (!string.IsNullOrWhiteSpace(region))
                    {
                        sql += " and a.region = ?";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(region))
                        {
                            cmd.Parameters.AddWithValue("?", region.Trim().ToUpperInvariant());
                        }

                        var dbValue = cmd.ExecuteScalar();
                        if (dbValue != DBNull.Value && dbValue != null)
                        {
                            result.TotalCount = Convert.ToInt32(dbValue);
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Ordinary Customers count");
                throw;
            }
        }
    }
}