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

                    string sql = @"SELECT EPF_NO, roleid, rolename, company, usertype 
                                   FROM REP_ROLE_NEW 
                                   WHERE usertype LIKE :userTypePattern 
                                   ORDER BY roleid";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("userTypePattern", userTypePattern);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                roles.Add(new RoleInfoModel
                                {
                                    EpfNo = reader["EPF_NO"]?.ToString(),
                                    RoleId = reader["ROLEID"]?.ToString(),
                                    RoleName = reader["ROLENAME"]?.ToString(),
                                    Company = reader["COMPANY"]?.ToString(),
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
                                USER_GROUP
                            )
                            VALUES
                            (
                                :epf_no,
                                :role_id,
                                :role_name,
                                :user_type,
                                :company,
                                :user_group
                            )";

                        using (var cmd = new OracleCommand(insertRoleSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;

                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = request.EpfNo?.Trim();
                            cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = request.RoleId?.Trim();
                            cmd.Parameters.Add("role_name", OracleDbType.Varchar2).Value = request.RoleName?.Trim();
                            cmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = request.UserType?.Trim();
                            cmd.Parameters.Add("company", OracleDbType.Varchar2).Value = request.Company?.Trim();
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

                        using (var cmd = new OracleCommand(insertRoleCctSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;

                            cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = request.RoleId?.Trim();
                            cmd.Parameters.Add("costcentre", OracleDbType.Varchar2).Value = request.CostCentre?.Trim();
                            cmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = request.LvlNo;

                            cmd.ExecuteNonQuery();
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

        public List<MotherCompanyOptionModel> GetMotherCompanies()
        {
            var companies = new List<MotherCompanyOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT DISTINCT TRIM(comp_id) AS CompanyId,
                                        TRIM(comp_nm) AS CompanyName
                        FROM glcompm
                        WHERE status = 2
                          AND comp_id IS NOT NULL
                        ORDER BY comp_id";

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
                        SELECT DISTINCT TRIM(d.dept_id) AS CostCentreId,
                                        TRIM(d.dept_nm) AS CostCentreName
                        FROM gldeptm d
                        WHERE d.status = 2
                          AND TRIM(d.comp_id) IN (
                                SELECT TRIM(comp_id)
                                FROM glcompm
                                WHERE status = 2
                                  AND (
                                        TRIM(comp_id) = :companyId
                                     OR TRIM(parent_id) = :companyId
                                     OR TRIM(grp_comp) = :companyId
                                  )
                          )
                        ORDER BY d.dept_id";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("companyId", OracleDbType.Varchar2).Value = companyId?.Trim();

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