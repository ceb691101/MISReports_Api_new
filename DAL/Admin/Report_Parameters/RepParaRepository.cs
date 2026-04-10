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

        public List<ReportParameterListItemModel> GetAllReportParams()
        {
            const string sql = @"
SELECT paraname,
       description,
       CASE
           WHEN populated = '1' THEN 'Yes'
           WHEN populated = '0' THEN 'No'
       END AS populated
FROM rep_report_params_new
ORDER BY paraname";

            var results = new List<ReportParameterListItemModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var populatedText = reader["POPULATED"]?.ToString()?.Trim();

                        results.Add(new ReportParameterListItemModel
                        {
                            ParaName = reader["PARANAME"]?.ToString()?.Trim(),
                            Description = reader["DESCRIPTION"]?.ToString()?.Trim(),
                            Populated = string.Equals(populatedText, "Yes", StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }

            return results;
        }

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
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        bool found;
                        using (var selectCmd = new OracleCommand(selectSql, conn))
                        {
                            selectCmd.BindByName = true;
                            selectCmd.Transaction = transaction;
                            selectCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                            var existing = selectCmd.ExecuteScalar();
                            found = existing != null && existing != DBNull.Value;
                        }

                        if (!found)
                        {
                            using (var insertCmd = new OracleCommand(insertSql, conn))
                            {
                                insertCmd.BindByName = true;
                                insertCmd.Transaction = transaction;
                                insertCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                                insertCmd.Parameters.Add("parm_ParaDesc", OracleDbType.Varchar2).Value = normalizedParaDesc;
                                insertCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

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
                            updateCmd.Transaction = transaction;
                            updateCmd.Parameters.Add("parm_ParaDesc", OracleDbType.Varchar2).Value = normalizedParaDesc;
                            updateCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                            updateCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return new SaveReportParamsResultModel
                        {
                            ParaName = normalizedParaId,
                            Found = true,
                            Inserted = false,
                            Updated = true
                        };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<ReportListItemModel> GetAllReports()
        {
            const string sql = @"
SELECT repid, paramlist
FROM rep_reports_new
ORDER BY repid";

            var results = new List<ReportListItemModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ReportListItemModel
                        {
                            RepId = reader["REPID"]?.ToString()?.Trim(),
                            ParamList = reader["PARAMLIST"]?.ToString()?.Trim() ?? string.Empty
                        });
                    }
                }
            }

            return results;
        }

        public PopulateParamTsResultModel PopulateParamTs(string repId, string paramList)
        {
            const string selectPendingParamsSql = @"
SELECT paraname
FROM rep_report_params_new
WHERE NVL(TRIM(populated), '0') = '0'
  AND TRIM(paraname) IS NOT NULL
ORDER BY paraid";

            const string selectAllReportsSql = @"
SELECT TRIM(repid) AS repid, TRIM(TO_CHAR(repid_no)) AS repid_no, paramlist
FROM rep_reports_new";

            const string selectReportSql = @"
SELECT paramlist
FROM rep_reports_new
WHERE UPPER(TRIM(repid)) = :parm_repid
   OR TRIM(TO_CHAR(repid_no)) = :parm_repid_no";

            const string updateReportByRepIdSql = @"
UPDATE rep_reports_new
SET paramlist = :param_list
WHERE UPPER(TRIM(repid)) = :parm_repid";

            const string updateReportByRepIdNoSql = @"
UPDATE rep_reports_new
SET paramlist = :param_list
WHERE TRIM(TO_CHAR(repid_no)) = :parm_repid_no";

            const string markParamsPopulatedSql = @"
UPDATE rep_report_params_new
SET populated = '1'
WHERE NVL(TRIM(populated), '0') = '0'";

            bool isAllReports = string.IsNullOrWhiteSpace(repId) || repId.Trim().Equals("ALL", StringComparison.OrdinalIgnoreCase);
            var candidateRepIds = new List<string>();

            if (!isAllReports)
            {
                candidateRepIds = (repId ?? string.Empty)
                    .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (candidateRepIds.Count == 0)
                {
                    candidateRepIds.Add(repId.Trim());
                }
            }

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var pendingParams = new List<string>();

                        using (var selectPendingCmd = new OracleCommand(selectPendingParamsSql, conn))
                        {
                            selectPendingCmd.BindByName = true;
                            selectPendingCmd.Transaction = transaction;

                            using (var reader = selectPendingCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var paramName = reader["PARANAME"]?.ToString()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(paramName))
                                    {
                                        pendingParams.Add(paramName);
                                    }
                                }
                            }
                        }

                        if (pendingParams.Count == 0)
                        {
                            transaction.Commit();
                            return new PopulateParamTsResultModel
                            {
                                UpdatedRows = 0,
                                Success = true,
                                ProcessedReports = 0,
                                ProcessedParams = 0,
                                AppendedParams = 0,
                                AlreadyExistingParams = 0
                            };
                        }

                        var reportsToProcess = new List<Tuple<string, string, string>>(); // repid, repid_no, paramlist
                        
                        if (isAllReports)
                        {
                            using (var cmd = new OracleCommand(selectAllReportsSql, conn))
                            {
                                cmd.Transaction = transaction;
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        reportsToProcess.Add(new Tuple<string, string, string>(
                                            reader["repid"]?.ToString(),
                                            reader["repid_no"]?.ToString(),
                                            reader["paramlist"]?.ToString()
                                        ));
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (var selectReportCmd = new OracleCommand(selectReportSql, conn))
                            {
                                selectReportCmd.BindByName = true;
                                selectReportCmd.Transaction = transaction;
                                
                                foreach (var candidateRepId in candidateRepIds)
                                {
                                    selectReportCmd.Parameters.Clear();
                                    selectReportCmd.Parameters.Add("parm_repid", OracleDbType.Varchar2).Value = candidateRepId.ToUpperInvariant();
                                    selectReportCmd.Parameters.Add("parm_repid_no", OracleDbType.Varchar2).Value = candidateRepId;

                                    var reportParamList = selectReportCmd.ExecuteScalar();
                                    if (reportParamList != null && reportParamList != DBNull.Value)
                                    {
                                        reportsToProcess.Add(new Tuple<string, string, string>(
                                            candidateRepId,
                                            candidateRepId,
                                            reportParamList.ToString()
                                        ));
                                        break; 
                                    }
                                }
                            }
                        }

                        if (reportsToProcess.Count == 0)
                        {
                            transaction.Commit();
                            return new PopulateParamTsResultModel
                            {
                                UpdatedRows = 0,
                                Success = false,
                                ProcessedParams = pendingParams.Count,
                                ProcessedReports = 0,
                                AppendedParams = 0,
                                AlreadyExistingParams = 0
                            };
                        }

                        var totalUpdatedRows = 0;
                        var totalAppendedParams = 0;
                        var totalAlreadyExistingCount = 0;

                        using (var updateReportByRepIdCmd = new OracleCommand(updateReportByRepIdSql, conn))
                        using (var updateReportByRepIdNoCmd = new OracleCommand(updateReportByRepIdNoSql, conn))
                        {
                            updateReportByRepIdCmd.BindByName = true;
                            updateReportByRepIdCmd.Transaction = transaction;
                            
                            updateReportByRepIdNoCmd.BindByName = true;
                            updateReportByRepIdNoCmd.Transaction = transaction;

                            foreach (var report in reportsToProcess)
                            {
                                var currentParamList = report.Item3?.Trim() ?? string.Empty;
                                var existingParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                                if (!string.IsNullOrWhiteSpace(currentParamList))
                                {
                                    var normalized = currentParamList;
                                    if (normalized.StartsWith("<"))
                                    {
                                        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, "^<[^>]*>&?", string.Empty);
                                    }

                                    foreach (var token in normalized.Split('&'))
                                    {
                                        var trimmedToken = token?.Trim();
                                        if (string.IsNullOrWhiteSpace(trimmedToken)) continue;

                                        var equalIndex = trimmedToken.IndexOf('=');
                                        var key = equalIndex >= 0 ? trimmedToken.Substring(0, equalIndex).Trim() : trimmedToken;

                                        if (!string.IsNullOrWhiteSpace(key)) existingParamNames.Add(key);
                                    }
                                }

                                var appendTokens = new List<string>();
                                foreach (var pendingParam in pendingParams)
                                {
                                    if (existingParamNames.Contains(pendingParam))
                                    {
                                        totalAlreadyExistingCount++;
                                    }
                                    else
                                    {
                                        appendTokens.Add(pendingParam + "=0");
                                        existingParamNames.Add(pendingParam);
                                        totalAppendedParams++;
                                    }
                                }

                                if (appendTokens.Count > 0)
                                {
                                    var finalParamList = currentParamList;
                                    if (string.IsNullOrWhiteSpace(finalParamList))
                                        finalParamList = string.Join("&", appendTokens);
                                    else
                                        finalParamList = finalParamList.TrimEnd('&') + "&" + string.Join("&", appendTokens);

                                    OracleCommand updateCmd;
                                    if (!string.IsNullOrEmpty(report.Item1) && report.Item1 != report.Item2)
                                    {
                                        updateCmd = updateReportByRepIdCmd;
                                        updateCmd.Parameters.Clear();
                                        updateCmd.Parameters.Add("param_list", OracleDbType.Varchar2).Value = finalParamList;
                                        updateCmd.Parameters.Add("parm_repid", OracleDbType.Varchar2).Value = report.Item1.ToUpperInvariant();
                                    }
                                    else
                                    {
                                        updateCmd = updateReportByRepIdNoCmd;
                                        updateCmd.Parameters.Clear();
                                        updateCmd.Parameters.Add("param_list", OracleDbType.Varchar2).Value = finalParamList;
                                        updateCmd.Parameters.Add("parm_repid_no", OracleDbType.Varchar2).Value = report.Item2;
                                    }

                                    totalUpdatedRows += updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        using (var markParamsCmd = new OracleCommand(markParamsPopulatedSql, conn))
                        {
                            markParamsCmd.BindByName = true;
                            markParamsCmd.Transaction = transaction;
                            markParamsCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return new PopulateParamTsResultModel
                        {
                            UpdatedRows = totalUpdatedRows,
                            Success = true,
                            ProcessedReports = reportsToProcess.Count,
                            ProcessedParams = pendingParams.Count,
                            AppendedParams = totalAppendedParams,
                            AlreadyExistingParams = totalAlreadyExistingCount
                        };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<ReportParameterListItemModel> GetPopedRepParams()
        {
            const string sql = @"
SELECT paraid, paraname, description
FROM rep_report_params_new
WHERE populated = '1'
ORDER BY paraid";

            var results = new List<ReportParameterListItemModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ReportParameterListItemModel
                        {
                            ParaId = reader["PARAID"]?.ToString()?.Trim(),
                            ParaName = reader["PARANAME"]?.ToString()?.Trim(),
                            Description = reader["DESCRIPTION"]?.ToString()?.Trim(),
                            Populated = true
                        });
                    }
                }
            }

            return results;
        }

        public DeleteReportParamResultModel DeleteReportParam(string paraId)
        {
            const string deleteSql = @"
DELETE FROM rep_report_params_new
WHERE UPPER(TRIM(paraname)) = :parm_ParaID";

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new OracleCommand(deleteSql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Transaction = transaction;
                            cmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = paraId?.Trim()?.ToUpperInvariant();

                            var deletedRows = cmd.ExecuteNonQuery();
                            transaction.Commit();

                            return new DeleteReportParamResultModel
                            {
                                DeletedRows = deletedRows
                            };
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
} 