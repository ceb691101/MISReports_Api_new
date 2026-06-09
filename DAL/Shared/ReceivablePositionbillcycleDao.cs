using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class ReceivablePositionBillCycleDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public BillCycleModel GetLast24BillCycles(string billType = null)
        {
            var model = new BillCycleModel();

            try
            {
                int? maxCycle = GetMaxBillCycle(billType);
                if (maxCycle.HasValue)
                {
                    model.MaxBillCycle = maxCycle.Value.ToString();
                    model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle.Value);
                }
                else
                {
                    model.ErrorMessage = "Error retrieving max bill cycle";
                }
            }
            catch (OleDbException ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error retrieving max bill cycle from receive_position: {ex.Message}");
                model.ErrorMessage = "Error retrieving max bill cycle";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
                model.ErrorMessage = "Unexpected error occurred";
            }

            return model;
        }

        private int? GetMaxBillCycle(string billType)
        {
            if (string.IsNullOrWhiteSpace(billType))
            {
                return FirstAvailableMax(
                    QueryMaxBillCycle(false),
                    QueryMaxBillCycle(true),
                    QueryMaxBillCycleFromMonTot(true));
            }

            if (UseBulkConnection(billType))
            {
                // Bulk receive_position may live on ordinary DB; mon_tot is the bulk fallback.
                return FirstAvailableMax(
                    QueryMaxBillCycle(true),
                    QueryMaxBillCycle(false),
                    QueryMaxBillCycleFromMonTot(true));
            }

            return FirstAvailableMax(
                QueryMaxBillCycle(false),
                QueryMaxBillCycle(true));
        }

        private static int? FirstAvailableMax(params int?[] values)
        {
            foreach (var value in values)
            {
                if (value.HasValue)
                    return value;
            }

            return null;
        }

        private int? QueryMaxBillCycleFromMonTot(bool useBulkConnection)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulkConnection))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand("SELECT MAX(bill_cycle) FROM mon_tot", conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value
                            && int.TryParse(maxCycleObj.ToString(), out int maxCycle))
                        {
                            return maxCycle;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"Could not read max bill cycle from mon_tot ({(useBulkConnection ? "bulk" : "ordinary")}): {ex.Message}");
            }

            return null;
        }

        private int? QueryMaxBillCycle(bool useBulkConnection)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulkConnection))
                {
                    conn.Open();

                    string sql = "SELECT MAX(bill_cycle) FROM receive_position";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value
                            && int.TryParse(maxCycleObj.ToString(), out int maxCycle))
                        {
                            return maxCycle;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"Could not read max bill cycle from {(useBulkConnection ? "bulk" : "ordinary")} DB: {ex.Message}");
            }

            return null;
        }

        private static bool UseBulkConnection(string billType)
        {
            return !string.IsNullOrWhiteSpace(billType)
                && billType.Trim().Equals("B", StringComparison.OrdinalIgnoreCase);
        }
    }
}