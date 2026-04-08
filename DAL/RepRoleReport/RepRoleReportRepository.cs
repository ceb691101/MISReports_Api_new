using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class RepRoleReportRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<RepRoleReportModel>> GetReportsByRole(string roleId)
        {
            var result = new List<RepRoleReportModel>();

            roleId = roleId.Trim().ToLower(); // match DB style like 'niro'

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
SELECT 
    rr.repid_no,
    rc.catname,
    rp.repname
FROM REP_ROLES_REP_NEW rr
JOIN REP_CATS_NEW rc 
    ON rr.catcode = rc.catcode
JOIN REP_REPORTS_NEW rp 
    ON rr.repid_no = rp.repid_no
   AND rr.repid = rp.repid
   AND rr.catcode = rp.catcode
WHERE 
    LOWER(rr.ROLEID) = :roleId
AND rr.favorite = 1
ORDER BY rc.catname, rp.repname";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new RepRoleReportModel
                            {
                                RepIdNo = reader["repid_no"]?.ToString(),
                                CategoryName = reader["catname"]?.ToString(),
                                ReportName = reader["repname"]?.ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}