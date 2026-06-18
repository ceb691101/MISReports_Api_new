using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    /// <summary>
    /// Retrieves the last 24 bill cycles from receive_position.
    ///
    /// CONNECTION ROUTING (same as ReceivablePositionDao):
    ///   bill_type = 'O'  →  InformixConnection     (GetConnection(false))
    ///   bill_type = 'B'  →  InformixBulkConnection (GetConnection(true))
    /// </summary>
    public class ReceivablePositionBillCycleDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        private static bool IsBulk(string billType)
            => !string.IsNullOrWhiteSpace(billType)
               && billType.Trim().Equals("B", StringComparison.OrdinalIgnoreCase);

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
                System.Diagnostics.Trace.WriteLine($"OleDb error reading max bill cycle: {ex.Message}");
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
            bool bulk = IsBulk(billType);

            // 1. Filtered by bill_type on the correct DB (most precise)
            // 2. Unfiltered on the correct DB (fallback if no typed rows yet)
            // 3. mon_tot on the correct DB (last resort)
            return FirstAvailableMax(
                QueryMaxBillCycle(bulk, billType),
                QueryMaxBillCycle(bulk, null),
                QueryMaxBillCycleFromMonTot(bulk));
        }

        private static int? FirstAvailableMax(params int?[] values)
        {
            foreach (var v in values)
                if (v.HasValue) return v;
            return null;
        }

        private int? QueryMaxBillCycle(bool useBulk, string billType = null)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulk))
                {
                    conn.Open();

                    bool filter = !string.IsNullOrWhiteSpace(billType);
                    string sql = filter
                        ? "SELECT MAX(bill_cycle) FROM receive_position WHERE bill_type = ?"
                        : "SELECT MAX(bill_cycle) FROM receive_position";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (filter)
                            cmd.Parameters.AddWithValue("?", billType.Trim().ToUpper());

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value
                            && int.TryParse(result.ToString(), out int max))
                            return max;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"Could not read max bill_cycle from receive_position " +
                    $"({(useBulk ? "bulk" : "ordinary")} DB): {ex.Message}");
            }
            return null;
        }

        private int? QueryMaxBillCycleFromMonTot(bool useBulk)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulk))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT MAX(bill_cycle) FROM mon_tot", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value
                            && int.TryParse(result.ToString(), out int max))
                            return max;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"Could not read max bill_cycle from mon_tot " +
                    $"({(useBulk ? "bulk" : "ordinary")} DB): {ex.Message}");
            }
            return null;
        }
    }
}