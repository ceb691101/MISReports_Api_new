using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Dashboard
{
    public class SolarBulkCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true);
        }

        public SolarBulkCustomersSummary GetSummary()
        {
            var summary = new SolarBulkCustomersSummary
            {
                TotalCustomers = 0,
                NetType1Customers = 0,
                NetType2Customers = 0,
                NetType3Customers = 0,
                NetType4Customers = 0,
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string targetCycle = GetMaxBillCycle(conn);
                    if (string.IsNullOrWhiteSpace(targetCycle))
                    {
                        summary.ErrorMessage = "No bill cycle found in netmtcons.";
                        return summary;
                    }

                    var groupedCounts = GetGroupedCountsFromNetmtcons(conn, targetCycle);

                    summary.NetType1Customers = GetCountByNetType(groupedCounts, "1");
                    summary.NetType2Customers = GetCountByNetType(groupedCounts, "2") + GetCountByNetType(groupedCounts, "5");
                    summary.NetType3Customers = GetCountByNetType(groupedCounts, "3");
                    summary.NetType4Customers = GetCountByNetType(groupedCounts, "4");
                    summary.TotalCustomers = summary.NetType1Customers
                                           + summary.NetType2Customers
                                           + summary.NetType3Customers
                                           + summary.NetType4Customers;
                }

                return summary;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar bulk customers summary");
                summary.ErrorMessage = ex.Message;
                return summary;
            }
        }

        public SolarBulkCustomersCount GetTotalCustomersCount()
        {
            return GetCountResult("ALL");
        }

        public SolarBulkCustomersCount GetNetType1CustomersCount()
        {
            return GetCountResult("1");
        }

        public SolarBulkCustomersCount GetNetType2CustomersCount()
        {
            return GetCountResult("2");
        }

        public SolarBulkCustomersCount GetNetType3CustomersCount()
        {
            return GetCountResult("3");
        }

        public SolarBulkCustomersCount GetNetType4CustomersCount()
        {
            return GetCountResult("4");
        }

        private SolarBulkCustomersCount GetCountResult(string netType)
        {
            var result = new SolarBulkCustomersCount
            {
                CustomersCount = 0,
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string targetCycle = GetMaxBillCycle(conn);
                    if (string.IsNullOrWhiteSpace(targetCycle))
                    {
                        result.ErrorMessage = "No bill cycle found in netmtcons.";
                        return result;
                    }

                    var groupedCounts = GetGroupedCountsFromNetmtcons(conn, targetCycle);

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
                logger.Error(ex, "Error while fetching solar bulk customers count");
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

        private Dictionary<string, int> GetGroupedCountsFromNetmtcons(OleDbConnection conn, string billCycle)
        {
            var groupedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            const string groupedSql = "SELECT bill_cycle, net_type, COUNT(*), SUM(gen_cap) " +
                                      "FROM netmtcons WHERE bill_cycle = ? GROUP BY 1,2 ORDER BY 2,1";

            using (var cmd = new OleDbCommand(groupedSql, conn))
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