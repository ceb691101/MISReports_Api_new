using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Inventory;
namespace MISReports_Api.DAL
{
    public class MaterialMasterDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        public List<MaterialMasterModel> GetMaterialMaster(string matCode)
        {
            var result = new List<MaterialMasterModel>();
            // NOTE: (status != 2 OR status != 3) is always true for any status value
            // (a row can't fail both at once), so as written this does not actually
            // restrict results to Active/Inactive only. Left matching the SQL as
            // received from the report spec — flag with your senior; likely meant
            // to be "status IN (2, 3)".
            const string query = @"
                SELECT A.mat_cd,
                       A.mat_nm,
                       A.maj_uom,
                       A.unit_price,
                       (CASE WHEN A.status = 2 THEN 'Active'
                             WHEN A.status = 3 THEN 'Inactive'
                             ELSE A.status || '- Unknown' END) AS status
                FROM inmatm A
                WHERE (A.mat_cd LIKE :matcode || '%' OR :matcode IS NULL)
                AND A.status IN (2, 3)
                ORDER BY A.mat_cd";
            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                // matCode may be null (blank input = return all records).
                if (string.IsNullOrWhiteSpace(matCode))
                {
                    cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2)
                    {
                        Value = DBNull.Value
                    });
                }
                else
                {
                    cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2)
                    {
                        Value = matCode.Trim()
                    });
                }

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MaterialMasterModel
                        {
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            MatNm = reader["mat_nm"] == DBNull.Value ? null : reader["mat_nm"].ToString(),
                            MajUom = reader["maj_uom"] == DBNull.Value ? null : reader["maj_uom"].ToString(),
                            UnitPrice = reader["unit_price"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["unit_price"]),
                            Status = reader["status"] == DBNull.Value ? null : reader["status"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}
