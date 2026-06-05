using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.Shared;
using System;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    /// <summary>
    /// Retrieves the maximum bill cycle from the <c>areas</c> table in the
    /// billsmry (bulk) database for a specific area, then generates the
    /// preceding 24 month/year strings for the dropdown.
    /// Used by: Arrears Position report.
    /// </summary>
    public class ArrearsPositionBillCycleDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public BillCycleModel GetMaxBillCycle(string areaCode)
        {
            var model = new BillCycleModel();

            using (var conn = _dbConnection.GetConnection(false)) // billsmry bulk connection
            {
                try
                {
                    // FIX 1: only open if not already open — prevents InvalidOperationException
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    const string sql = "SELECT MAX(bill_cycle) FROM areas WHERE area_code = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // FIX 2: OleDb ignores parameter names — use positional name for clarity
                        cmd.Parameters.AddWithValue("p1", areaCode);

                        object maxCycleObj = cmd.ExecuteScalar();

                        if (maxCycleObj != null && maxCycleObj != DBNull.Value)
                        {
                            if (int.TryParse(maxCycleObj.ToString(), out int maxCycle))
                            {
                                model.MaxBillCycle = maxCycle.ToString();
                                model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle);
                            }
                            else
                            {
                                model.ErrorMessage = "Max bill cycle value could not be parsed.";
                            }
                        }
                        else
                        {
                            model.ErrorMessage = "No bill cycle found for the specified area.";
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"OleDbException in ArrearsPositionBillCycleDao.GetMaxBillCycle: {ex.Message}");
                    model.ErrorMessage = "Database error while retrieving max bill cycle.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Unexpected error in ArrearsPositionBillCycleDao.GetMaxBillCycle: {ex.Message}");
                    model.ErrorMessage = "Unexpected error occurred while retrieving max bill cycle.";
                }
            }

            return model;
        }
    }
}