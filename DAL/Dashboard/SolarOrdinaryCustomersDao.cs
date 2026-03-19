using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Dashboard
{
    public class SolarOrdinaryCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public SolarOrdinaryCustomersSummary GetSummary(string billCycle = null)
        {
            var summary = new SolarOrdinaryCustomersSummary
            {
                BillCycle = string.Empty,
                TotalCustomers = 0,
                NetMeteringCustomers = 0,
                NetAccountingCustomers = 0,
                NetPlusCustomers = 0,
                NetPlusPlusCustomers = 0,
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string targetCycle = string.IsNullOrWhiteSpace(billCycle)
                        ? GetMaxBillCycle(conn)
                        : billCycle.Trim();

                    summary.BillCycle = targetCycle;

                    if (string.IsNullOrWhiteSpace(targetCycle))
                    {
                        summary.ErrorMessage = "No bill cycle found in netmtcons.";
                        return summary;
                    }

                    summary.TotalCustomers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ?",
                        targetCycle);

                    summary.NetMeteringCustomers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type = '1'",
                        targetCycle);

                    summary.NetAccountingCustomers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type IN ('2', '5')",
                        targetCycle);

                    summary.NetPlusCustomers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type = '3'",
                        targetCycle);

                    summary.NetPlusPlusCustomers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type = '4'",
                        targetCycle);
                }

                return summary;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar ordinary customer summary");
                summary.ErrorMessage = ex.Message;
                return summary;
            }
        }

        public string GetLatestBillCycle()
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    return GetMaxBillCycle(conn);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching latest bill cycle from netmtcons");
                return string.Empty;
            }
        }

        public SolarOrdinaryCustomersCount GetTotalCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle,
                "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ?");
        }

        public SolarOrdinaryCustomersCount GetNetMeteringCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle,
                "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type = '1'");
        }

        public SolarOrdinaryCustomersCount GetNetAccountingCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle,
                "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type IN ('2', '5')");
        }

        public SolarOrdinaryCustomersCount GetNetPlusCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle,
                "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type = '3'");
        }

        public SolarOrdinaryCustomersCount GetNetPlusPlusCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle,
                "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND net_type = '4'");
        }

        private SolarOrdinaryCustomersCount GetCountResult(string billCycle, string sql)
        {
            var result = new SolarOrdinaryCustomersCount
            {
                BillCycle = string.Empty,
                CustomersCount = 0,
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string targetCycle = string.IsNullOrWhiteSpace(billCycle)
                        ? GetMaxBillCycle(conn)
                        : billCycle.Trim();

                    result.BillCycle = targetCycle;

                    if (string.IsNullOrWhiteSpace(targetCycle))
                    {
                        result.ErrorMessage = "No bill cycle found in netmtcons.";
                        return result;
                    }

                    result.CustomersCount = ExecuteCount(conn, sql, targetCycle);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar ordinary customers count");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private string GetMaxBillCycle(OleDbConnection conn)
        {
            const string sql = "SELECT MAX(bill_cycle) FROM netmtcons";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : result.ToString().Trim();
            }
        }

        private int ExecuteCount(OleDbConnection conn, string sql, string billCycle)
        {
            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", billCycle);
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            }
        }
    }
}