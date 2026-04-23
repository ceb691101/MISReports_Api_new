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

        public OrdinaryCustomers GetOrdinaryCustomersCount(string currentBillCycle)
        {
            var result = new OrdinaryCustomers { TotalCount = 0, BillCycle = currentBillCycle };

            try
            {
                logger.Info($"=== START GetOrdinaryCustomersCount for Cycle: {currentBillCycle} ===");

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

                    int targetCycle = maxBillCycle - 2;
                    result.BillCycle = targetCycle.ToString();

                    string sql = @"select sum(cnt)
                                from consmry
                                where bill_cycle = (select max(bill_cycle) from areas) - 2";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
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