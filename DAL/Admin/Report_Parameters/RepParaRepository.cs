using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Admin.Report_Parameters;

namespace MISReports_Api.DAL.Admin.Report_Parameters
{
    public class RepParaRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["OracleTest"].ConnectionString;

        public SaveReportParamsResultModel SaveReportParams(string paraId, string paraDesc)
        {
            const string selectSql = @"
SELECT paraname
FROM rep_report_params_new
WHERE paraname = :parm_ParaID";

            const string insertSql = @"
INSERT INTO rep_report_params_new (paraid, paraname, description, populated)
VALUES (0, :parm_ParaID, :parm_ParaDesc, 0)";

            const string updateSql = @"
UPDATE rep_report_params_new
SET description = :parm_ParaDesc
WHERE paraname = :parm_ParaID";

            var normalizedParaId = paraId?.Trim();
            var normalizedParaDesc = paraDesc?.Trim();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                bool found;
                using (var selectCmd = new OracleCommand(selectSql, conn))
                {
                    selectCmd.BindByName = true;
                    selectCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                    var existing = selectCmd.ExecuteScalar();
                    found = existing != null && existing != DBNull.Value;
                }

                if (!found)
                {
                    using (var insertCmd = new OracleCommand(insertSql, conn))
                    {
                        insertCmd.BindByName = true;
                        insertCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                        insertCmd.Parameters.Add("parm_ParaDesc", OracleDbType.Varchar2).Value = normalizedParaDesc;
                        insertCmd.ExecuteNonQuery();
                    }

                    return new SaveReportParamsResultModel
                    {
                        ParaName = normalizedParaId,
                        Found = false,
                        Inserted = true,
                        Updated = false
                    };
                }

                using (var updateCmd = new OracleCommand(updateSql, conn))
                {
                    updateCmd.BindByName = true;
                    updateCmd.Parameters.Add("parm_ParaDesc", OracleDbType.Varchar2).Value = normalizedParaDesc;
                    updateCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                    updateCmd.ExecuteNonQuery();
                }

                return new SaveReportParamsResultModel
                {
                    ParaName = normalizedParaId,
                    Found = true,
                    Inserted = false,
                    Updated = true
                };
            }
        }

        public PopulateParamTsResultModel PopulateParamTs(string repId, string paramList)
        {
            const string updateSql = @"
UPDATE rep_reports_new1
SET paramlist = :param_list
WHERE repid = :parm_repid";

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(updateSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("param_list", OracleDbType.Varchar2).Value = paramList?.Trim();
                    cmd.Parameters.Add("parm_repid", OracleDbType.Varchar2).Value = repId?.Trim();

                    return new PopulateParamTsResultModel
                    {
                        UpdatedRows = cmd.ExecuteNonQuery()
                    };
                }
            }
        }
    }
}