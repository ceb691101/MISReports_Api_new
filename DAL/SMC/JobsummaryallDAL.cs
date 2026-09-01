using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class JobSummaryAllDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Overflow-safe decimal reader, same pattern used on the other reports (unbounded
        // NUMBER columns can exceed .NET decimal's ~28-29 digit range and make
        // Convert.ToDecimal throw).
        private static decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            OracleDecimal od = reader.GetOracleDecimal(ordinal);
            try
            {
                od = OracleDecimal.SetPrecision(od, 28);
                return od.Value;
            }
            catch (OverflowException)
            {
                return (decimal)(double)od;
            }
        }

        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            Type fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(decimal))
            {
                return SafeGetDecimal(reader, columnName)?.ToString();
            }

            return reader.GetValue(ordinal)?.ToString();
        }

        public List<JobSummaryAllModel> GetJobSummaryAll(DateTime fromDate, DateTime toDate)
        {
            var result = new List<JobSummaryAllModel>();

            const string query = @"
                SELECT  pc.dept_id,
                        pc.estimate_no,
                        pc.project_no,
                        w.phase,
                        w.connection_type,
                        sp.total_cost AS Std_cost,
                        pc.std_cost AS actual_cost,
                        pc.descr
                FROM      speststd sp, pcesthmt pc, wiring_land_detail w, applications ap
                WHERE     TRIM(sp.estimate_no) = TRIM(pc.estimate_no)
                  AND     ap.application_id = w.application_id
                  AND     sp.estimate_no = ap.application_no
                  AND     pc.PRJ_ASS_DT >= :fromdate
                  AND     pc.PRJ_ASS_DT <  :todateexcl
                  AND     ap.application_type = 'NC'
                ORDER BY 1";

            // Notes vs. the original query:
            // 1. Dates are bound as real OracleDbType.Date parameters instead of
            //    TO_DATE(:param,'yyyy/mm/dd') string parsing, and toDate is treated as
            //    inclusive of the whole day (PRJ_ASS_DT < toDate + 1 day).
            // 2. No dept_id/comp_id bind-variable comparisons exist in this query (no
            //    :costctr parameter is used at all), so the CHAR blank-padding fix applied
            //    on the other reports doesn't apply here -- the only join/filter columns
            //    are estimate_no, application_id, and application_no, none of which are
            //    compared against a bind variable.
            // 3. Column naming is reproduced exactly as given: sp.total_cost (from the
            //    original standard estimate table) is aliased Std_cost, while pc.std_cost
            //    (PCESTHMT's own cost field) is aliased actual_cost. That looks
            //    intentional -- pc.std_cost likely reflects the job's current/updated
            //    cost while sp.total_cost is the original estimate -- but worth
            //    double-checking that the aliasing matches what you actually want
            //    "Standard Cost" vs "Actual Cost" to mean in the report.
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new JobSummaryAllModel
                        {
                            DeptId = SafeGetString(reader, "dept_id"),
                            EstimateNo = SafeGetString(reader, "estimate_no"),
                            ProjectNo = SafeGetString(reader, "project_no"),
                            Phase = SafeGetString(reader, "phase"),
                            ConnectionType = SafeGetString(reader, "connection_type"),
                            StdCost = SafeGetDecimal(reader, "Std_cost"),
                            ActualCost = SafeGetDecimal(reader, "actual_cost"),
                            Descr = SafeGetString(reader, "descr")
                        });
                    }
                }
            }

            return result;
        }
    }
}