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
                          WHERE TRIM(gldeptm.dept_id) = TRIM(:costctr)
                            AND gldeptm.comp_id = glcompm.comp_id) AS cct_name
                FROM      glsubbal A, glsubacm B
                WHERE     TRIM(A.gl_cd) = TRIM(:glcd)
                  AND A.yr_ind = :repyear
                  AND A.mth_ind = :repmonth
                  AND A.sub_ac = B.sub_ac
                  AND A.gl_cd = B.gl_cd";

            // gl_cd and dept_id are fixed-length CHAR columns (CHAR(27) / CHAR(6)) that
            // Oracle stores blank-padded to their full declared length. A bind variable
            // (OracleDbType.Varchar2) forces NON-padded comparison semantics, so it never
            // matches the blank-padded stored value -- that's why literal SQL worked
            // (literals are CHAR, so blank-padded comparison kicks in automatically) but
            // the parameterized query returned 0 rows. Wrapping both sides in TRIM()
            // removes the padding difference regardless of comparison semantics.
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string glCd = costCtrTrimmed + "-L9100";

            // yr_ind / mth_ind are NUMBER columns, so bind them as numbers rather than
            // strings to avoid relying on implicit conversion.
            int repYearNum = int.Parse((repYear ?? string.Empty).Trim());
            int repMonthNum = int.Parse((repMonth ?? string.Empty).Trim());

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("glcd", OracleDbType.Varchar2) { Value = glCd });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Int32) { Value = repYearNum });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Int32) { Value = repMonthNum });

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