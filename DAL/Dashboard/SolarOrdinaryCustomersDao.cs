using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

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

        public SolarOrdinaryGenerationCapacityGraph GetGenerationCapacityGraph(string billCycle = null, int cycles = 12)
        {
            var graph = new SolarOrdinaryGenerationCapacityGraph
            {
                MaxBillCycle = string.Empty,
                SelectedBillCycle = string.Empty,
                AvailableBillCycles = new List<string>(),
                Records = new List<SolarOrdinaryGenerationCapacityPoint>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string maxBillCycleText = GetMaxBillCycle(conn);
                    if (!int.TryParse(maxBillCycleText, out int maxBillCycle))
                    {
                        graph.ErrorMessage = "No bill cycle found in netprogrs.";
                        return graph;
                    }

                    int latestCompletedBillCycle = maxBillCycle - 1;
                    if (latestCompletedBillCycle <= 0)
                    {
                        graph.ErrorMessage = "No completed bill cycle found in netprogrs.";
                        return graph;
                    }

                    int safeCycles = cycles <= 0 ? 12 : cycles;
                    int selectedBillCycle = ResolveRequestedBillCycle(billCycle, latestCompletedBillCycle);
                    var availableBillCycles = GetAvailableBillCyclesFromNetprogrs(conn, latestCompletedBillCycle, safeCycles);

                    if (!availableBillCycles.Contains(selectedBillCycle) && availableBillCycles.Count > 0)
                    {
                        selectedBillCycle = availableBillCycles[0];
                    }

                    graph.MaxBillCycle = latestCompletedBillCycle.ToString();
                    graph.SelectedBillCycle = selectedBillCycle.ToString();
                    graph.AvailableBillCycles = availableBillCycles.Select(cycle => cycle.ToString()).ToList();
                    graph.Records = GetGenerationCapacityByCycleFromNetprogrs(conn, graph.SelectedBillCycle);
                }

                return graph;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar ordinary generation capacity graph");
                graph.ErrorMessage = ex.Message;
                return graph;
            }
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

        private List<int> GetAvailableBillCyclesFromNetprogrs(OleDbConnection conn, int maxAllowedCycle, int takeCount)
        {
            var billCycles = new List<int>();

            const string sql = "SELECT DISTINCT bill_cycle FROM netprogrs WHERE bill_cycle <= ? ORDER BY bill_cycle DESC";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", maxAllowedCycle.ToString());

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read() && billCycles.Count < takeCount)
                    {
                        if (reader[0] == DBNull.Value)
                        {
                            continue;
                        }

                        string billCycleText = reader[0].ToString().Trim();
                        if (int.TryParse(billCycleText, out int billCycleValue) && billCycleValue > 0)
                        {
                            billCycles.Add(billCycleValue);
                        }
                    }
                }
            }

            return billCycles;
        }

        private List<SolarOrdinaryGenerationCapacityPoint> GetGenerationCapacityByCycleFromNetprogrs(OleDbConnection conn, string billCycle)
        {
            var groupedByDisplayType = new Dictionary<string, SolarOrdinaryGenerationCapacityPoint>(StringComparer.OrdinalIgnoreCase);

            const string sql = "SELECT bill_cycle, net_type, SUM(cnt), SUM(tot_gen_cap) " +
                               "FROM netprogrs WHERE bill_cycle = ? GROUP BY 1,2 ORDER BY 2,1";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", billCycle);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string rawNetType = reader[1] == DBNull.Value ? string.Empty : reader[1].ToString().Trim();
                        string displayNetType = GetNetTypeDisplayName(rawNetType);

                        if (string.IsNullOrWhiteSpace(displayNetType))
                        {
                            continue;
                        }

                        int accountsCount = reader[2] == DBNull.Value ? 0 : Convert.ToInt32(reader[2]);
                        decimal capacityKw = reader[3] == DBNull.Value ? 0m : Convert.ToDecimal(reader[3]);

                        if (!groupedByDisplayType.TryGetValue(displayNetType, out SolarOrdinaryGenerationCapacityPoint point))
                        {
                            point = new SolarOrdinaryGenerationCapacityPoint
                            {
                                BillCycle = billCycle,
                                NetType = displayNetType,
                                AccountsCount = 0,
                                CapacityKw = 0m
                            };

                            groupedByDisplayType[displayNetType] = point;
                        }

                        point.AccountsCount += accountsCount;
                        point.CapacityKw += capacityKw;
                    }
                }
            }

            return groupedByDisplayType.Values
                                     .OrderByDescending(item => item.CapacityKw)
                                     .ThenBy(item => item.NetType)
                                     .ToList();
        }

        private static int ResolveRequestedBillCycle(string requestedBillCycle, int fallbackBillCycle)
        {
            if (!string.IsNullOrWhiteSpace(requestedBillCycle) &&
                int.TryParse(requestedBillCycle.Trim(), out int parsedBillCycle) &&
                parsedBillCycle > 0)
            {
                return parsedBillCycle;
            }

            return fallbackBillCycle;
        }

        private static string GetNetTypeDisplayName(string netType)
        {
            switch (netType)
            {
                case "1": return "Net Metering";
                case "2":
                case "5": return "Net Accounting";
                case "3": return "Net Plus";
                case "4": return "Net Plus Plus";
                default: return string.Empty;
            }
        }
    }
}