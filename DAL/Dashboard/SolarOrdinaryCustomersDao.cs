using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Collections.Generic;
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

                    string targetCycle = ResolveTargetBillCycle(conn, billCycle);

                    summary.BillCycle = targetCycle;

                    if (string.IsNullOrWhiteSpace(targetCycle))
                    {
                        summary.ErrorMessage = "No bill cycle found in netprogrs.";
                        return summary;
                    }

                    var groupedCounts = GetGroupedCountsFromNetprogrs(conn, targetCycle);

                    summary.NetMeteringCustomers = GetCountByNetType(groupedCounts, "1");
                    summary.NetAccountingCustomers = GetCountByNetType(groupedCounts, "2") + GetCountByNetType(groupedCounts, "5");
                    summary.NetPlusCustomers = GetCountByNetType(groupedCounts, "3");
                    summary.NetPlusPlusCustomers = GetCountByNetType(groupedCounts, "4");
                    summary.TotalCustomers = summary.NetMeteringCustomers
                                           + summary.NetAccountingCustomers
                                           + summary.NetPlusCustomers
                                           + summary.NetPlusPlusCustomers;
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
                    return ResolveTargetBillCycle(conn, null);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching latest bill cycle from netprogrs");
                return string.Empty;
            }
        }

        public SolarOrdinaryCustomersCount GetTotalCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle, "ALL");
        }

        public SolarOrdinaryCustomersCount GetNetMeteringCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle, "1");
        }

        public SolarOrdinaryCustomersCount GetNetAccountingCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle, "2");
        }

        public SolarOrdinaryCustomersCount GetNetPlusCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle, "3");
        }

        public SolarOrdinaryCustomersCount GetNetPlusPlusCustomersCount(string billCycle = null)
        {
            return GetCountResult(billCycle, "4");
        }

        private SolarOrdinaryCustomersCount GetCountResult(string billCycle, string netType)
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

                    string targetCycle = ResolveTargetBillCycle(conn, billCycle);

                    result.BillCycle = targetCycle;

                    if (string.IsNullOrWhiteSpace(targetCycle))
                    {
                        result.ErrorMessage = "No bill cycle found in netprogrs.";
                        return result;
                    }

                    var groupedCounts = GetGroupedCountsFromNetprogrs(conn, targetCycle);

                    if (netType == "ALL")
                    {
                        result.CustomersCount = GetCountByNetType(groupedCounts, "1")
                                              + GetCountByNetType(groupedCounts, "2")
                                              + GetCountByNetType(groupedCounts, "5")
                                              + GetCountByNetType(groupedCounts, "3")
                                              + GetCountByNetType(groupedCounts, "4");
                    }
                    else if (netType == "2")
                    {
                        result.CustomersCount = GetCountByNetType(groupedCounts, "2") + GetCountByNetType(groupedCounts, "5");
                    }
                    else
                    {
                        result.CustomersCount = GetCountByNetType(groupedCounts, netType);
                    }
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
            const string sql = "SELECT MAX(bill_cycle) FROM netprogrs";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : result.ToString().Trim();
            }
        }

        private string ResolveTargetBillCycle(OleDbConnection conn, string billCycle)
        {
            if (!string.IsNullOrWhiteSpace(billCycle))
            {
                return billCycle.Trim();
            }

            string maxBillCycleText = GetMaxBillCycle(conn);
            if (!int.TryParse(maxBillCycleText, out int maxBillCycle))
            {
                return string.Empty;
            }

            int targetCycle = maxBillCycle - 1;
            return targetCycle <= 0 ? string.Empty : targetCycle.ToString();
        }

        private Dictionary<string, int> GetGroupedCountsFromNetprogrs(OleDbConnection conn, string billCycle)
        {
            var groupedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            const string sql = "SELECT bill_cycle, net_type, SUM(cnt), SUM(tot_gen_cap) " +
                               "FROM netprogrs WHERE bill_cycle = ? GROUP BY 1,2 ORDER BY 2,1";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", billCycle);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string netType = reader[1] == DBNull.Value ? string.Empty : reader[1].ToString().Trim();

                        if (string.IsNullOrWhiteSpace(netType))
                        {
                            continue;
                        }

                        int count = reader[2] == DBNull.Value ? 0 : Convert.ToInt32(reader[2]);
                        groupedCounts[netType] = count;
                    }
                }
            }

            return groupedCounts;
        }

        private int GetCountByNetType(Dictionary<string, int> groupedCounts, string netType)
        {
            return groupedCounts.TryGetValue(netType, out int count) ? count : 0;
        }
    }
}