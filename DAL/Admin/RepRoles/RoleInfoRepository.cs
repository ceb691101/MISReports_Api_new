using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class RoleInfoRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["OracleTest"].ConnectionString;

        private List<string> NormalizeCostCentres(CreateRoleRequest request)
        {
            var normalized = new List<string>();

            if (request?.CostCentres != null)
            {
                foreach (var costCentre in request.CostCentres)
                {
                    var trimmed = costCentre?.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && !normalized.Contains(trimmed))
                    {
                        normalized.Add(trimmed);
                    }
                }
            }

            var singleCostCentre = request?.CostCentre?.Trim();
            if (!string.IsNullOrWhiteSpace(singleCostCentre) && !normalized.Contains(singleCostCentre))
            {
                normalized.Add(singleCostCentre);
            }

            return normalized;
        }

        private int GetCostCentreLvlNo(OracleConnection conn, OracleTransaction transaction, string costCentreId)
        {
            try
            {
                const string sql = @"
                    SELECT lvl_no
                    FROM glcompm
                    WHERE TRIM(comp_id) = :cost_centre_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Transaction = transaction;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("cost_centre_id", OracleDbType.Varchar2).Value = costCentreId?.Trim();

                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out var lvlNo))
                    {
                        return lvlNo;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCostCentreLvlNo: {ex.Message}");
            }

            return 0; // Default value if not found
        }

        private string NormalizeUserType(string userType)
        {
            var t = userType?.Trim();
            if (string.Equals(t, "ADMINISTRATOR", StringComparison.OrdinalIgnoreCase))
            {
                return "ADMIN";
            }

            if (string.Equals(t, "USER", StringComparison.OrdinalIgnoreCase))
            {
                return "USER";
            }

            return t ?? string.Empty;
        }

        public List<RoleInfoModel> GetAdminRoles()
        {
            return GetRolesByUserType("ADMIN%");
        }

        public List<RoleInfoModel> GetUserRoles()
        {
            return GetRolesByUserType("USER%");
        }

        private List<RoleInfoModel> GetRolesByUserType(string userTypePattern)
        {
            var roles = new List<RoleInfoModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT r.EPF_NO,
                                                                                    r.roleid,
                                                                                    r.rolename,
                                                                                    r.company,
                                                                                    r.mcompany,
                                                                                    r.usertype,
                                                                                    r.user_group,
                                                                                    NVL(LISTAGG(c.costcentre, ',') WITHIN GROUP (ORDER BY c.costcentre), '') AS costcentre
                                                                     FROM REP_ROLE_NEW r
                                                                     LEFT JOIN REP_ROLES_CCT_NEW c ON TRIM(c.roleid) = TRIM(r.roleid)
                                                                     WHERE r.usertype LIKE :userTypePattern
                                                                     GROUP BY r.EPF_NO, r.roleid, r.rolename, r.company, r.mcompany, r.usertype, r.user_group
                                                                     ORDER BY r.roleid";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("userTypePattern", userTypePattern);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var costCentreValue = reader["COSTCENTRE"]?.ToString() ?? string.Empty;

                                roles.Add(new RoleInfoModel
                                {
                                    EpfNo = reader["EPF_NO"]?.ToString(),
                                    RoleId = reader["ROLEID"]?.ToString(),
                                    RoleName = reader["ROLENAME"]?.ToString(),
                                    Company = reader["COMPANY"]?.ToString(),
                                    MotherCompany = reader["MCOMPANY"]?.ToString(),
                                    UserGroup = reader["USER_GROUP"]?.ToString(),
                                    CostCentre = costCentreValue,
                                    CostCentres = string.IsNullOrWhiteSpace(costCentreValue)
                                        ? new List<string>()
                                        : new List<string>(costCentreValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)),
                                    UserType = reader["USERTYPE"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRolesByUserType: {ex.Message}");
                throw;
            }

            return roles;
        }
        public bool CreateRole(CreateRoleRequest request)
        {
            var costCentres = NormalizeCostCentres(request);

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string insertRoleSql = @"
                            INSERT INTO REP_ROLE_NEW
                            (
                                EPF_NO,
                                ROLEID,
                                ROLENAME,
                                USERTYPE,
                                COMPANY,
                                MCOMPANY,
                                USER_GROUP
                            )
                            VALUES
                            (
                                :epf_no,
                                :role_id,
                                :role_name,
                                :user_type,
                                :company,
                                :mcompany,
                                :user_group
                            )";

                        using (var cmd = new OracleCommand(insertRoleSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;

                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = request.EpfNo?.Trim();
                            cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = request.RoleId?.Trim();
                            cmd.Parameters.Add("role_name", OracleDbType.Varchar2).Value = request.RoleName?.Trim();
                            cmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = NormalizeUserType(request.UserType);
                            cmd.Parameters.Add("company", OracleDbType.Varchar2).Value = request.Company?.Trim();
                            cmd.Parameters.Add("mcompany", OracleDbType.Varchar2).Value = request.MotherCompany?.Trim();
                            cmd.Parameters.Add("user_group", OracleDbType.Varchar2).Value = request.UserGroup?.Trim();

                            cmd.ExecuteNonQuery();
                        }

                        const string insertRoleCctSql = @"
                            INSERT INTO REP_ROLES_CCT_NEW
                            (
                                ROLEID,
                                COSTCENTRE,
                                LVL_NO,
                                STATUS
                            )
                            VALUES
                            (
                                :role_id,
                                :costcentre,
                                :lvl_no,
                                2
                            )";

                        foreach (var costCentre in costCentres)
                        {
                            int lvlNo = GetCostCentreLvlNo(conn, transaction, costCentre);

                            using (var cmd = new OracleCommand(insertRoleCctSql, conn))
                            {
                                cmd.Transaction = transaction;
                                cmd.BindByName = true;

                                cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = request.RoleId?.Trim();
                                cmd.Parameters.Add("costcentre", OracleDbType.Varchar2).Value = costCentre;
                                cmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in CreateRole: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public bool UpdateRole(CreateRoleRequest request)
        {
            var costCentres = NormalizeCostCentres(request);

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string checkRoleSql = @"
                            SELECT COUNT(1)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :original_epf_no";

                        using (var checkCmd = new OracleCommand(checkRoleSql, conn))
                        {
                            checkCmd.Transaction = transaction;
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("original_epf_no", OracleDbType.Varchar2).Value = request.OriginalEpfNo?.Trim();

                            var roleCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (roleCount == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        const string updateRoleSql = @"
                            UPDATE REP_ROLE_NEW
                            SET EPF_NO = :epf_no,
                                ROLEID = :new_role_id,
                                ROLENAME = :role_name,
                                USERTYPE = :user_type,
                                COMPANY = :company,
                                MCOMPANY = :mcompany,
                                USER_GROUP = :user_group
                            WHERE TRIM(EPF_NO) = :original_epf_no";

                        using (var cmd = new OracleCommand(updateRoleSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;

                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = request.EpfNo?.Trim();
                            cmd.Parameters.Add("new_role_id", OracleDbType.Varchar2).Value = request.RoleId?.Trim();
                            cmd.Parameters.Add("role_name", OracleDbType.Varchar2).Value = request.RoleName?.Trim();
                            cmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = NormalizeUserType(request.UserType);
                            cmd.Parameters.Add("company", OracleDbType.Varchar2).Value = request.Company?.Trim();
                            cmd.Parameters.Add("mcompany", OracleDbType.Varchar2).Value = request.MotherCompany?.Trim();
                            cmd.Parameters.Add("user_group", OracleDbType.Varchar2).Value = request.UserGroup?.Trim();
                            cmd.Parameters.Add("original_epf_no", OracleDbType.Varchar2).Value = request.OriginalEpfNo?.Trim();

                            cmd.ExecuteNonQuery();
                        }

                        const string deleteRoleCctSql = @"
                            DELETE FROM REP_ROLES_CCT_NEW
                            WHERE TRIM(ROLEID) = (
                                SELECT TRIM(ROLEID)
                                FROM REP_ROLE_NEW
                                WHERE TRIM(EPF_NO) = :original_epf_no
                            )";

                        using (var deleteCmd = new OracleCommand(deleteRoleCctSql, conn))
                        {
                            deleteCmd.Transaction = transaction;
                            deleteCmd.BindByName = true;
                            deleteCmd.Parameters.Add("original_epf_no", OracleDbType.Varchar2).Value = request.OriginalEpfNo?.Trim();
                            deleteCmd.ExecuteNonQuery();
                        }

                        const string insertRoleCctSql = @"
                            INSERT INTO REP_ROLES_CCT_NEW
                            (
                                ROLEID,
                                COSTCENTRE,
                                LVL_NO,
                                STATUS
                            )
                            VALUES
                            (
                                :role_id,
                                :costcentre,
                                :lvl_no,
                                2
                            )";

                        foreach (var costCentre in costCentres)
                        {
                            int lvlNo = GetCostCentreLvlNo(conn, transaction, costCentre);

                            using (var insertCmd = new OracleCommand(insertRoleCctSql, conn))
                            {
                                insertCmd.Transaction = transaction;
                                insertCmd.BindByName = true;

                                insertCmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = request.RoleId?.Trim();
                                insertCmd.Parameters.Add("costcentre", OracleDbType.Varchar2).Value = costCentre;
                                insertCmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;

                                insertCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in UpdateRole: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public bool DeleteRole(string epfNo)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string checkRoleSql = @"
                            SELECT COUNT(1)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no";

                        using (var checkCmd = new OracleCommand(checkRoleSql, conn))
                        {
                            checkCmd.Transaction = transaction;
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo?.Trim();

                            var roleCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (roleCount == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        const string deleteRoleCctSql = @"
                            DELETE FROM REP_ROLES_CCT_NEW
                            WHERE TRIM(ROLEID) = (
                                SELECT TRIM(ROLEID)
                                FROM REP_ROLE_NEW
                                WHERE TRIM(EPF_NO) = :epf_no
                            )";

                        using (var cmd = new OracleCommand(deleteRoleCctSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo?.Trim();
                            cmd.ExecuteNonQuery();
                        }

                        const string deleteRoleSql = @"
                            DELETE FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no";

                        using (var cmd = new OracleCommand(deleteRoleSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo?.Trim();

                            var affectedRows = cmd.ExecuteNonQuery();
                            if (affectedRows == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }

                            transaction.Commit();
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in DeleteRole: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public List<MotherCompanyOptionModel> GetMotherCompanies()
        {
            var companies = new List<MotherCompanyOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                                SELECT comp_id AS CompanyId,
                                             comp_nm AS CompanyName
                                FROM glcompm
                                WHERE status = 2
                                ORDER BY comp_nm";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                companies.Add(new MotherCompanyOptionModel
                                {
                                    CompanyId = reader["CompanyId"]?.ToString(),
                                    CompanyName = reader["CompanyName"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMotherCompanies: {ex.Message}");
                throw;
            }

            return companies;
        }

        public List<CostCentreOptionModel> GetCostCentresByCompany(string companyId)
        {
            var costCentres = new List<CostCentreOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                                SELECT dept_id AS CostCentreId,
                                             dept_nm AS CostCentreName
                                FROM gldeptm
                                WHERE comp_id = :compid
                                ORDER BY dept_id";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("compid", OracleDbType.Varchar2).Value = companyId?.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                costCentres.Add(new CostCentreOptionModel
                                {
                                    CostCentreId = reader["CostCentreId"]?.ToString(),
                                    CostCentreName = reader["CostCentreName"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCostCentresByCompany: {ex.Message}");
                throw;
            }

            return costCentres;
        }
    }
}