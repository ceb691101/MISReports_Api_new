using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class MaterialCommittedStockRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Get Provinces
        public async Task<List<MaterialCommittedStockProvinceModel>> GetProvinces()
        {
            var result = new List<MaterialCommittedStockProvinceModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
SELECT DISTINCT g.comp_id AS COMP_ID,
                c.comp_nm AS COMP_NM
FROM gldeptm g
JOIN glcompm c ON c.comp_id = g.comp_id
ORDER BY g.comp_id";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new MaterialCommittedStockProvinceModel
                        {
                            CompId = reader["COMP_ID"]?.ToString().Trim(),
                            CompNm = reader["COMP_NM"]?.ToString().Trim()
                        });
                    }
                }
            }

            return result;
        }

        // Get Material Committed Stock
        public async Task<List<MaterialCommittedStockModel>> GetMaterialCommittedStock(string compId, string matCode = null)
        {
            var resultList = new List<MaterialCommittedStockModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Oracle ODP.NET silently returns 0 rows when named bind variables
                // are reused across nested correlated subquery boundaries in this query.
                // Safe fix: sanitize inputs (strip single quotes) and inline as literals.
                string safeCompId = compId.Replace("'", "").Trim();
                bool hasMatCode = !string.IsNullOrWhiteSpace(matCode);
                string matCodeClause = hasMatCode
                    ? $"AND T1.mat_cd LIKE '{matCode.Replace("'", "").Trim()}%'"
                    : "";

                string sql = $@"
SELECT T1.MAT_CD,
       T2.MAT_NM,
       SUM(T1.QTY_ON_HAND) AS COMMITED_COST,
       (T1.dept_id || '-' || (SELECT dept_nm FROM gldeptm WHERE T1.DEPT_ID = dept_id)) AS C8,
       (SELECT dept_nm FROM gldeptm WHERE dept_id = T1.DEPT_ID) AS AREA,
       T1.UOM_CD,
       (SELECT comp_nm FROM glcompm WHERE comp_id = '{safeCompId}') AS REGION
FROM INMATM T2, INWRHMTM T1
WHERE T2.MAT_CD = T1.MAT_CD
AND T1.QTY_ON_HAND > 0
AND T1.DEPT_ID IN (
    SELECT dept_id FROM gldeptm
    WHERE comp_id IN (
        SELECT comp_id FROM glcompm WHERE comp_id = '{safeCompId}'
    )
)
AND T1.GRADE_CD = 'NEW'
AND T1.status = 2
{matCodeClause}
GROUP BY T1.MAT_CD, T2.MAT_NM, T1.UOM_CD, T1.dept_id
ORDER BY 1 ASC, 2 ASC, 5 ASC, 4 ASC";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        resultList.Add(new MaterialCommittedStockModel
                        {
                            MatCd = reader["MAT_CD"]?.ToString().Trim(),
                            MatNm = reader["MAT_NM"]?.ToString().Trim(),
                            CommittedCost = reader["COMMITED_COST"] != DBNull.Value
                                ? Convert.ToDecimal(reader["COMMITED_COST"]) : 0,
                            DeptInfo = reader["C8"]?.ToString().Trim(),
                            Area = reader["AREA"]?.ToString().Trim(),
                            UomCd = reader["UOM_CD"]?.ToString().Trim(),
                            Region = reader["REGION"]?.ToString().Trim()
                        });
                    }
                }
            }

            return resultList;
        }


    }
}