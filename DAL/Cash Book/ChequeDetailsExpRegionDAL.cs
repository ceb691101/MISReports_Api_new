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
    public class ChqDetailsExpRegionDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ChqDetailsExpRegionModel> GetChqDetailsExpRegion(string fromDate, string toDate, string compId, string glCode)
        {
            var result = new List<ChqDetailsExpRegionModel>();
            const string query = @"
                SELECT A.dept_id, A.chq_dt, A.chq_no, A.pymt_docno, D.exp_cd,
                       (CASE WHEN D.dr_amt > 0 THEN D.dr_amt
                             WHEN D.cr_amt > 0 THEN (-1) * D.cr_amt
                             ELSE NULL END) AS dr_amt,
                       B.chq_amt, A.payee, SUBSTR(C.remarks,1,150) AS remarks,
                       A.chq_run, A1.run_dt,
                       (SELECT comp_nm FROM glcompm WHERE comp_id = :compid) AS CCT_NAME
                FROM cbchqdmt A, cbchqrgh B, cbpmthmt C, cbchqhmt A1, cbpmtemt D
                WHERE A.pymt_docno = C.doc_no
                  AND A.pymt_docno = D.doc_no
                  AND A.pymt_docpf = C.doc_pf
                  AND TRIM(A.chq_run) = TRIM(C.chq_run)
                  AND TRIM(A.chq_run) = TRIM(A1.chq_run)
                  AND TRIM(A.chq_run) = TRIM(B.chq_run)
                  AND TRIM(A.chq_no) = TRIM(B.chq_no)
                  AND TRIM(A.chq_no) = TRIM(C.chq_no)
                  AND C.doc_pf = D.doc_pf
                  AND C.dept_id = D.dept_id
                  AND A.dept_id IN (
                      SELECT dept_id
                      FROM gldeptm
                      WHERE comp_id IN (
                          SELECT comp_id
                          FROM glcompm
                          WHERE comp_id = :compid
                             OR parent_id = :compid
                             OR grp_comp = :compid
                      )
                  )
                  AND D.exp_cd LIKE '%'||:glcode||'%'
                  AND (A.chq_dt >= TO_DATE(:fromdate,'yyyy/mm/dd') AND A.chq_dt <= TO_DATE(:todate,'yyyy/mm/dd'))
                  AND (D.dr_amt > 0 OR D.cr_amt > 0)
                ORDER BY A.dept_id, D.exp_cd, A.chq_no, A.grp_id";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid (used 4 times) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("glcode", OracleDbType.Varchar2) { Value = glCode ?? "" });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ChqDetailsExpRegionModel
                        {
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            PymtDocNo = reader["pymt_docno"] == DBNull.Value ? null : reader["pymt_docno"].ToString(),
                            ExpCd = reader["exp_cd"] == DBNull.Value ? null : reader["exp_cd"].ToString(),
                            DrAmt = reader["dr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["dr_amt"]),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            ChqRun = reader["chq_run"] == DBNull.Value ? null : reader["chq_run"].ToString(),
                            RunDt = reader["run_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["run_dt"]),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}