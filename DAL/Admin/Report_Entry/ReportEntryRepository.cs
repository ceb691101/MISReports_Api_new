using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class ReportEntryRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["OracleTest"].ConnectionString;

        public int GetNextReportIdNo()
        {
            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand("SELECT REP_REPORTS_SEQ.NEXTVAL FROM DUAL", conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetNextReportIdNo: {ex.Message}");
                throw;
            }
        }

        public bool AddReportEntry(ReportEntryModel request)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var repIdNo = request.RepIdNo > 0 ? request.RepIdNo : GetNextReportIdNo();
                        var favorite = request.Favorite == 1 ? 1 : 0;
                        var active = request.Active == 1 ? 1 : 0;
                        if (active == 0)
                        {
                            favorite = 0;
                        }

                        const string sql = @"
                            INSERT INTO REP_REPORTS_NEW
                            (REPID_NO, REPID, CATCODE, REPNAME, FAVORITE, ACTIVE)
                            VALUES
                            (:repid_no, :repid, :catcode, :repname, :favorite, :active)";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                            cmd.Parameters.Add("repid", OracleDbType.Varchar2).Value = request.RepId?.Trim();
                            cmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = request.CatCode?.Trim();
                            cmd.Parameters.Add("repname", OracleDbType.Varchar2).Value = request.RepName?.Trim();
                            cmd.Parameters.Add("favorite", OracleDbType.Int32).Value = favorite;
                            cmd.Parameters.Add("active", OracleDbType.Int32).Value = active;

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in AddReportEntry: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public string GetDeleteStatus(int repIdNo, string catCode)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                const string existsSql = @"
                    SELECT COUNT(1)
                    FROM REP_REPORTS_NEW
                    WHERE REPID_NO = :repid_no
                    AND CATCODE = :catcode";

                using (var existsCmd = new OracleCommand(existsSql, conn))
                {
                    existsCmd.BindByName = true;
                    existsCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                    existsCmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = catCode?.Trim();
                    var existsCount = Convert.ToInt32(existsCmd.ExecuteScalar());
                    if (existsCount > 0)
                    {
                        const string restrictedExactSql = @"
                            SELECT COUNT(1)
                            FROM REP_REPORTS_NEW r
                            INNER JOIN DACONS16.REP_ROLES_REP_NEW rr
                                ON rr.REPID = r.REPID
                               AND rr.CATCODE = r.CATCODE
                            WHERE r.REPID_NO = :repid_no
                            AND r.CATCODE = :catcode";

                        using (var restrictedExactCmd = new OracleCommand(restrictedExactSql, conn))
                        {
                            restrictedExactCmd.BindByName = true;
                            restrictedExactCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                            restrictedExactCmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = catCode?.Trim();
                            var restrictedExactCount = Convert.ToInt32(restrictedExactCmd.ExecuteScalar());
                            return restrictedExactCount > 0 ? "restricted" : "ok";
                        }
                    }
                }

                const string countByRepIdSql = @"
                    SELECT COUNT(1)
                    FROM REP_REPORTS_NEW
                    WHERE REPID_NO = :repid_no";

                using (var countByRepIdCmd = new OracleCommand(countByRepIdSql, conn))
                {
                    countByRepIdCmd.BindByName = true;
                    countByRepIdCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                    var countByRepId = Convert.ToInt32(countByRepIdCmd.ExecuteScalar());

                    if (countByRepId == 0)
                    {
                        return "not_found";
                    }

                    if (countByRepId > 1)
                    {
                        return "ambiguous";
                    }
                }

                const string restrictedFallbackSql = @"
                    SELECT COUNT(1)
                    FROM REP_REPORTS_NEW r
                    INNER JOIN DACONS16.REP_ROLES_REP_NEW rr
                        ON rr.REPID = r.REPID
                       AND rr.CATCODE = r.CATCODE
                    WHERE r.REPID_NO = :repid_no";

                using (var restrictedFallbackCmd = new OracleCommand(restrictedFallbackSql, conn))
                {
                    restrictedFallbackCmd.BindByName = true;
                    restrictedFallbackCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                    var restrictedFallbackCount = Convert.ToInt32(restrictedFallbackCmd.ExecuteScalar());
                    return restrictedFallbackCount > 0 ? "restricted" : "ok";
                }
            }
        }

        public bool EditReportEntry(int repIdNo, string currentCatCode, ReportEntryModel request)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var favorite = request.Favorite == 1 ? 1 : 0;
                        var active = request.Active == 1 ? 1 : 0;
                        if (active == 0)
                        {
                            favorite = 0;
                        }

                        const string sql = @"
                            UPDATE REP_REPORTS_NEW
                            SET 
                                CATCODE  = :catcode,
                                REPNAME  = :repname,
                                FAVORITE = :favorite,
                                ACTIVE   = :active
                            WHERE 
                                REPID_NO = :repid_no
                                AND CATCODE = :current_catcode";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = request.CatCode?.Trim();
                            cmd.Parameters.Add("repname", OracleDbType.Varchar2).Value = request.RepName?.Trim();
                            cmd.Parameters.Add("favorite", OracleDbType.Int32).Value = favorite;
                            cmd.Parameters.Add("active", OracleDbType.Int32).Value = active;
                            cmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                            cmd.Parameters.Add("current_catcode", OracleDbType.Varchar2).Value = currentCatCode?.Trim();

                            var affectedRows = cmd.ExecuteNonQuery();
                            if (affectedRows == 0)
                            {
                                const string countSql = @"
                                    SELECT COUNT(1)
                                    FROM REP_REPORTS_NEW
                                    WHERE REPID_NO = :repid_no";

                                using (var countCmd = new OracleCommand(countSql, conn))
                                {
                                    countCmd.Transaction = transaction;
                                    countCmd.BindByName = true;
                                    countCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                                    var countByRepIdNo = Convert.ToInt32(countCmd.ExecuteScalar());

                                    if (countByRepIdNo == 1)
                                    {
                                        const string fallbackSql = @"
                                            UPDATE REP_REPORTS_NEW
                                            SET 
                                                CATCODE  = :catcode,
                                                REPNAME  = :repname,
                                                FAVORITE = :favorite,
                                                ACTIVE   = :active
                                            WHERE REPID_NO = :repid_no";

                                        using (var fallbackCmd = new OracleCommand(fallbackSql, conn))
                                        {
                                            fallbackCmd.Transaction = transaction;
                                            fallbackCmd.BindByName = true;
                                            fallbackCmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = request.CatCode?.Trim();
                                            fallbackCmd.Parameters.Add("repname", OracleDbType.Varchar2).Value = request.RepName?.Trim();
                                            fallbackCmd.Parameters.Add("favorite", OracleDbType.Int32).Value = favorite;
                                            fallbackCmd.Parameters.Add("active", OracleDbType.Int32).Value = active;
                                            fallbackCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                                            affectedRows = fallbackCmd.ExecuteNonQuery();
                                        }
                                    }
                                }

                                if (affectedRows == 0)
                                {
                                    transaction.Rollback();
                                    return false; // Not found
                                }
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in EditReportEntry: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public bool DeleteReportEntry(int repIdNo, string catCode)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string sql = @"
                            DELETE FROM REP_REPORTS_NEW
                            WHERE REPID_NO = :repid_no
                            AND CATCODE = :catcode";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                            cmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = catCode?.Trim();

                            var affectedRows = cmd.ExecuteNonQuery();
                            if (affectedRows == 0)
                            {
                                const string countSql = @"
                                    SELECT COUNT(1)
                                    FROM REP_REPORTS_NEW
                                    WHERE REPID_NO = :repid_no";

                                using (var countCmd = new OracleCommand(countSql, conn))
                                {
                                    countCmd.Transaction = transaction;
                                    countCmd.BindByName = true;
                                    countCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                                    var countByRepIdNo = Convert.ToInt32(countCmd.ExecuteScalar());

                                    if (countByRepIdNo == 1)
                                    {
                                        const string fallbackDeleteSql = @"
                                            DELETE FROM REP_REPORTS_NEW
                                            WHERE REPID_NO = :repid_no";

                                        using (var fallbackDeleteCmd = new OracleCommand(fallbackDeleteSql, conn))
                                        {
                                            fallbackDeleteCmd.Transaction = transaction;
                                            fallbackDeleteCmd.BindByName = true;
                                            fallbackDeleteCmd.Parameters.Add("repid_no", OracleDbType.Int32).Value = repIdNo;
                                            affectedRows = fallbackDeleteCmd.ExecuteNonQuery();
                                        }
                                    }
                                }

                                if (affectedRows == 0)
                                {
                                    transaction.Rollback();
                                    return false; // Not found
                                }
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in DeleteReportEntry: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public List<ReportEntryModel> FilterReportEntries(string repid, string catcode)
        {
            var results = new List<ReportEntryModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT REPID_NO, REPID, CATCODE, REPNAME, FAVORITE, ACTIVE
                        FROM REP_REPORTS_NEW
                        WHERE REPID = :repid
                        AND CATCODE = :catcode";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("repid", OracleDbType.Varchar2).Value = repid?.Trim();
                        cmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = catcode?.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new ReportEntryModel
                                {
                                    RepIdNo = reader["REPID_NO"] != DBNull.Value ? Convert.ToInt32(reader["REPID_NO"]) : 0,
                                    RepId = reader["REPID"]?.ToString()?.Trim(),
                                    CatCode = reader["CATCODE"]?.ToString()?.Trim(),
                                    RepName = reader["REPNAME"]?.ToString()?.Trim(),
                                    Favorite = reader["FAVORITE"] != DBNull.Value ? Convert.ToInt32(reader["FAVORITE"]) : 0,
                                    Active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FilterReportEntries: {ex.Message}");
                throw;
            }

            return results;
        }

        public List<ReportEntryModel> GetAllReportEntries()
        {
            var results = new List<ReportEntryModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT REPID_NO, REPID, CATCODE, REPNAME, FAVORITE, ACTIVE
                        FROM REP_REPORTS_NEW
                        ORDER BY REPID_NO";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new ReportEntryModel
                                {
                                    RepIdNo = reader["REPID_NO"] != DBNull.Value ? Convert.ToInt32(reader["REPID_NO"]) : 0,
                                    RepId = reader["REPID"]?.ToString()?.Trim(),
                                    CatCode = reader["CATCODE"]?.ToString()?.Trim(),
                                    RepName = reader["REPNAME"]?.ToString()?.Trim(),
                                    Favorite = reader["FAVORITE"] != DBNull.Value ? Convert.ToInt32(reader["FAVORITE"]) : 0,
                                    Active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllReportEntries: {ex.Message}");
                throw;
            }

            return results;
        }
    }
}
