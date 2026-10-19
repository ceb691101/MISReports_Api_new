using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class BillCycleFromAreaDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public BillCycleModel GetLast24BillCycles()
        {
            var model = new BillCycleModel();

            using (var conn = _dbConnection.GetConnection(false))
            {
                try
                {
                    conn.Open();

                    // Use the latest bill cycle recorded in areas.
                    const string maxCycleSql = "SELECT MAX(bill_cycle) FROM areas";
                    int maxCycle;

                    using (OleDbCommand cmd = new OleDbCommand(maxCycleSql, conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj == null || maxCycleObj == DBNull.Value)
                        {
                            model.ErrorMessage = "No bill cycles found in areas table";
                            return model;
                        }

                        if (!int.TryParse(maxCycleObj.ToString(), out maxCycle))
                        {
                            model.ErrorMessage = "Invalid bill cycle format";
                            return model;
                        }
                    }

                    model.MaxBillCycle = maxCycle.ToString();
                    model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle);
                }
                
                catch (OleDbException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Error retrieving bill cycle: {ex.Message}");
                    model.ErrorMessage = "Error retrieving bill cycle";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
                    model.ErrorMessage = "Unexpected error occurred";
                }
            }

            return model;
        }
    }
}
