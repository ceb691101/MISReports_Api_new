using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PhysicalVerification
{
    public class PHVValidationRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private const string PrimarySql = @"
                SELECT DISTINCT
                    T1.MAT_CD,
                    T2.MAT_NM,
                    T4.UOM_CD,
                    T4.GRADE_CD,
                    T1.QTY_ON_HAND,
                    T1.CNTED_QTY,
                    T4.UNIT_PRICE,
                    T1.REASON
                FROM
                    INPHVDTT T1,
                    INMATM   T2,
                    INPHVHTT T3,
                    INWRHMTM T4
                WHERE
                    T3.DOC_NO   = T1.DOC_NO
                    AND T3.DOC_PF   = T1.DOC_PF
                    AND T1.MAT_CD   = T2.MAT_CD
                    AND T1.MAT_CD   = T4.MAT_CD
                    AND T1.GRADE_CD = T4.GRADE_CD
                    AND TRIM(T3.DEPT_ID) = :dept_id
                    AND TRIM(T4.DEPT_ID) = :dept_id
                    AND TRIM(T1.DEPT_ID) = :dept_id
                    AND TO_CHAR(T3.PHV_DT,'YYYY') = :rep_year
                    AND TO_CHAR(T3.PHV_DT,'MM')   = :rep_month
                ORDER BY
                    T1.MAT_CD,
                    T4.GRADE_CD";

        private const string FallbackSql = @"
                SELECT DISTINCT
                    T1.MAT_CD,
                    T2.MAT_NM,
                    T4.UOM_CD,
                    T4.GRADE_CD,
                    T1.QTY_ON_HAND,
                    T1.CNTED_QTY,
                    T5.UNIT_COST AS UNIT_PRICE,
                    T1.REASON
                FROM
                    INPHVDMT T1
                    JOIN INPOSTMT T5
                        ON T1.DOC_NO = T5.DOC_NO
                       AND T1.DOC_PF = T5.DOC_PF
                       AND T1.DEPT_ID = T5.DEPT_ID
                       AND T1.MAT_CD = T5.MAT_CD
                       AND T1.GRADE_CD = T5.GRADE_CD
                    JOIN INMATM T2
                        ON T5.MAT_CD = T2.MAT_CD
                    JOIN INPHVHMT T3
                        ON T3.DOC_NO = T1.DOC_NO
                       AND T3.DOC_PF = T1.DOC_PF
                       AND T3.DEPT_ID = T1.DEPT_ID
                       AND T3.BATCH_ID = T1.BATCH_ID
                    JOIN INWRHMTM T4
                        ON T5.DEPT_ID = T4.DEPT_ID
                       AND T5.MAT_CD = T4.MAT_CD
                       AND T5.GRADE_CD = T4.GRADE_CD
                       AND T3.WRH_CD = T4.WRH_CD
                WHERE
                    TRIM(T1.DEPT_ID) = :dept_id
                    AND TO_CHAR(T3.PHV_DT,'YYYY') = :rep_year
                    AND TO_CHAR(T3.PHV_DT,'MM')   = :rep_month
                    AND T4.STATUS IN (7)
                ORDER BY
                    T1.MAT_CD,
                    T4.GRADE_CD";

        public async Task<List<PHVValidationModel>> GetPHVValidationDataAsync(
            string deptId,
            string repYear,
            string repMonth)
        {
            var normalizedDeptId = (deptId ?? string.Empty).Trim();
            var normalizedRepYear = (repYear ?? string.Empty).Trim();
            var normalizedRepMonth = NormalizeMonth(repMonth);

            var result = await ReadValidationRowsAsync(
                PrimarySql,
                normalizedDeptId,
                normalizedRepYear,
                normalizedRepMonth);

            if (result.Count > 0)
            {
                return result;
            }

            return await ReadValidationRowsAsync(
                FallbackSql,
                normalizedDeptId,
                normalizedRepYear,
                normalizedRepMonth);
        }

        private static string NormalizeMonth(string repMonth)
        {
            if (int.TryParse((repMonth ?? string.Empty).Trim(), out var month) && month >= 1 && month <= 12)
            {
                return month.ToString("D2");
            }

            return (repMonth ?? string.Empty).Trim();
        }

        private async Task<List<PHVValidationModel>> ReadValidationRowsAsync(
            string sql,
            string deptId,
            string repYear,
            string repMonth)
        {
            var result = new List<PHVValidationModel>();

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId;
                cmd.Parameters.Add("rep_year", OracleDbType.Varchar2).Value = repYear;
                cmd.Parameters.Add("rep_month", OracleDbType.Varchar2).Value = repMonth;

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new PHVValidationModel
                        {
                            MatCd = reader["MAT_CD"]?.ToString().Trim(),
                            MatNm = reader["MAT_NM"]?.ToString().Trim(),
                            UomCd = reader["UOM_CD"]?.ToString().Trim(),
                            GradeCd = reader["GRADE_CD"]?.ToString().Trim(),
                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value
                                ? Convert.ToDecimal(reader["QTY_ON_HAND"])
                                : 0,
                            CntedQty = reader["CNTED_QTY"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CNTED_QTY"])
                                : 0,
                            UnitPrice = reader["UNIT_PRICE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["UNIT_PRICE"])
                                : 0,
                            Reason = reader["REASON"]?.ToString().Trim()
                        });
                    }
                }
            }

            return result;
        }
    }
}