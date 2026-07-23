using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CurrentAcctBalCCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CurrentAcctBalCCModel> GetCurrentAcctBalCC(string repYear, string repMonth, string costCtr)
        {
            var result = new List<CurrentAcctBalCCModel>();

            const string query = @"
                SELECT  A.sub_ac,
                        B.ac_nm,
                        A.cl_bal,
                        (SELECT comp_nm
                           FROM glcompm, gldeptm
                          WHERE gldeptm.dept_id = :costctr
                            AND gldeptm.comp_id = glcompm.comp_id) AS cct_name
                FROM      glsubbal A, glsubacm B
                WHERE     A.gl_cd = :costctr || '-L9100'
                  AND A.yr_ind = :repyear
                  AND A.mth_ind = :repmonth
                  AND A.sub_ac = B.sub_ac
                  AND A.gl_cd = B.gl_cd";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Varchar2) { Value = repYear });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Varchar2) { Value = repMonth });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CurrentAcctBalCCModel
                        {
                            SubAc = reader["sub_ac"] == DBNull.Value ? null : reader["sub_ac"].ToString(),
                            AcNm = reader["ac_nm"] == DBNull.Value ? null : reader["ac_nm"].ToString(),
                            ClBal = reader["cl_bal"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cl_bal"]),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}