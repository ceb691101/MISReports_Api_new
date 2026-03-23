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

        public bool AddReportEntry(ReportEntryModel request)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string sql = @"
                            INSERT INTO REP_REPORTS_NEW
                            (REPID_NO, REPID, CATCODE, REPNAME, FAVORITE, ACTIVE)
                            VALUES
                            (REP_REPORTS_SEQ.NEXTVAL, :repid, :catcode, :repname, :favorite, :active)";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("repid", OracleDbType.Varchar2).Value = request.RepId?.Trim();
                            cmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = request.CatCode?.Trim();
                            cmd.Parameters.Add("repname", OracleDbType.Varchar2).Value = request.RepName?.Trim();
                            cmd.Parameters.Add("favorite", OracleDbType.Int32).Value = request.Favorite;
                            cmd.Parameters.Add("active", OracleDbType.Int32).Value = request.Active;

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

        public bool EditReportEntry(ReportEntryModel request)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string sql = @"
                            UPDATE REP_REPORTS_NEW
                            SET 
                                CATCODE  = :catcode,
                                REPNAME  = :repname,
                                FAVORITE = :favorite,
                                ACTIVE   = :active
                            WHERE 
                                REPID = :repid";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("catcode", OracleDbType.Varchar2).Value = request.CatCode?.Trim();
                            cmd.Parameters.Add("repname", OracleDbType.Varchar2).Value = request.RepName?.Trim();
                            cmd.Parameters.Add("favorite", OracleDbType.Int32).Value = request.Favorite;
                            cmd.Parameters.Add("active", OracleDbType.Int32).Value = request.Active;
                            cmd.Parameters.Add("repid", OracleDbType.Varchar2).Value = request.RepId?.Trim();

                            var affectedRows = cmd.ExecuteNonQuery();
                            if (affectedRows == 0)
                            {
                                transaction.Rollback();
                                return false; // Not found
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

        public bool DeleteReportEntry(string repid)
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
                            WHERE REPID = :repid
                            AND NOT EXISTS (
                                SELECT 1 
                                FROM DACONS16.REP_ROLES_REP_NEW 
                                WHERE REPID = :repid
                            )";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("repid", OracleDbType.Varchar2).Value = repid?.Trim();

                            var affectedRows = cmd.ExecuteNonQuery();
                            if (affectedRows == 0)
                            {
                                transaction.Rollback();
                                return false; // Not found or constraints failed
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
                        SELECT REPID, CATCODE, REPNAME, FAVORITE, ACTIVE
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
                        SELECT REPID, CATCODE, REPNAME, FAVORITE, ACTIVE
                        FROM REP_REPORTS_NEW
                        ORDER BY REPID";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new ReportEntryModel
                                {
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
