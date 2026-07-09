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

        // Get Material Committed Stock — flat detail rows
        public async Task<List<MaterialCommittedStockModel>> GetMaterialCommittedStock(string compId, string matCode = null)
        {
            var resultList = new List<MaterialCommittedStockModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Sanitize inputs — strip single quotes and inline as literals.
                // Oracle ODP.NET silently returns 0 rows when named bind variables
                // are reused across nested correlated subquery boundaries in this query.
                string safeCompId = compId.Replace("'", "").Trim();
                bool hasMatCode = !string.IsNullOrWhiteSpace(matCode);
                string matCodeClause = hasMatCode
                    ? $"AND T1.mat_cd LIKE '{matCode.Replace("'", "").Trim()}%'"
                    : "";

                string sql = $@"
SELECT T1.DEPT_ID,
       T1.WRH_CD,
       T1.MAT_CD,
       T2.MAT_NM,
       T1.GRADE_CD,
       SUBSTR(T1.MAT_CD, 1, 2) AS MAJOR,
       T1.QTY_ON_HAND,
       T1.UNIT_PRICE,
       (T1.QTY_ON_HAND * T1.UNIT_PRICE) AS VALUE
FROM INMATM T2, INWRHMTM T1
WHERE T2.MAT_CD = T1.MAT_CD
  AND T1.QTY_ON_HAND >= 0
  AND T1.DEPT_ID IN (
      SELECT dept_id FROM gldeptm
      WHERE comp_id IN (
          SELECT comp_id FROM glcompm WHERE comp_id = '{safeCompId}' OR parent_id = '{safeCompId}'
      )
  )
  AND T1.GRADE_CD = 'NEW'
  AND T1.status = 2
  {matCodeClause}
ORDER BY T1.DEPT_ID, T1.WRH_CD, T1.MAT_CD";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        resultList.Add(new MaterialCommittedStockModel
                        {
                            DeptId = reader["DEPT_ID"]?.ToString().Trim(),
                            WrhCd = reader["WRH_CD"]?.ToString().Trim(),
                            MatCd = reader["MAT_CD"]?.ToString().Trim(),
                            MatNm = reader["MAT_NM"]?.ToString().Trim(),
                            GradeCd = reader["GRADE_CD"]?.ToString().Trim(),
                            Major = reader["MAJOR"]?.ToString().Trim(),
                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value
                                ? Convert.ToDecimal(reader["QTY_ON_HAND"]) : 0,
                            UnitPrice = reader["UNIT_PRICE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["UNIT_PRICE"]) : 0,
                            Value = reader["VALUE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["VALUE"]) : 0
                        });
                    }
                }
            }

            return resultList;
        }


    }
}