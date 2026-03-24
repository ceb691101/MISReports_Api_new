using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class CostCenterRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        /// <summary>
        /// Get company details
        /// </summary>
        public CompanyModel GetCompanyDetails(string companyId)
        {
            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            TRIM(comp_id) as CompanyId,
                            comp_nm as CompanyName,
                            TRIM(NVL(parent_id, '')) as ParentId,
                            TRIM(NVL(grp_comp, '')) as GroupCompany
                        FROM glcompm
                        WHERE TRIM(comp_id) LIKE :comp_prefix
                          AND status = 2
                        FETCH FIRST 1 ROW ONLY";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        string compPrefix = (companyId?.Trim().ToUpper() ?? "") + "%";
                        cmd.Parameters.Add("comp_prefix", OracleDbType.Varchar2).Value = compPrefix;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string compId = reader["CompanyId"]?.ToString();
                                string compName = reader["CompanyName"]?.ToString();

                                return new CompanyModel
                                {
                                    CompanyId = compId,
                                    CompanyName = compName,
                                    CompanyDisplay = $"{compId} : {compName}",
                                    ParentId = reader["ParentId"]?.ToString(),
                                    GroupCompany = reader["GroupCompany"]?.ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCompanyDetails: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Load all cost centers for a given company using the exact department query
        /// </summary>
        public List<CostCenterModel> GetCostCentersForCompany(string companyId)
        {
            var costCenters = new List<CostCenterModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            dept_id || ':' || dept_nm as DeptDisplay,
                            TRIM(dept_id) as DeptId,
                            TRIM(dept_nm) as DeptName
                        FROM gldeptm
                        WHERE dept_id IN (
                            SELECT dept_id
                            FROM gldeptm
                            WHERE status = 2
                              AND comp_id IN (
                                  SELECT comp_id
                                  FROM glcompm
                                  WHERE status = 2
                                    AND (
                                        comp_id   LIKE :comp_prefix
                                     OR parent_id LIKE :comp_prefix
                                     OR grp_comp  LIKE :comp_prefix
                                    )
                              )
                        )
                        ORDER BY dept_nm";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        string compPrefix = (companyId?.Trim().ToUpper() ?? "") + "%";
                        cmd.Parameters.Add("comp_prefix", OracleDbType.Varchar2).Value = compPrefix;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string deptDisplay = reader["DeptDisplay"]?.ToString();
                                string deptId = reader["DeptId"]?.ToString();
                                string deptName = reader["DeptName"]?.ToString();
                                
                                costCenters.Add(new CostCenterModel
                                {
                                    CostCenterId = deptId,
                                    CostCenterName = deptName,
                                    CostCenterDisplay = deptDisplay,
                                    LevelNo = 0,
                                    IsSelected = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCostCentersForCompany: {ex.Message}");
                throw new Exception($"Failed to load cost centers: {ex.Message}", ex);
            }

            return costCenters;
        }

        /// <summary>
        /// Load cost centers assigned to a specific role
        /// </summary>
        public List<string> GetAssignedCostCenters(string roleId)
        {
            var assignedCostCenters = new List<string>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT DISTINCT TRIM(costcentre) as CostCenterId
                        FROM rep_roles_cct
                        WHERE TRIM(roleid) = :role_id
                        ORDER BY costcentre";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId?.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var costCenterId = reader["CostCenterId"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(costCenterId))
                                {
                                    assignedCostCenters.Add(costCenterId);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAssignedCostCenters: {ex.Message}");
                throw new Exception($"Failed to load assigned cost centers: {ex.Message}", ex);
            }

            return assignedCostCenters;
        }

        /// <summary>
        /// Get cost center level number
        /// </summary>
        private int GetCostCenterLevelNo(OracleConnection conn, string costCenterId)
        {
            try
            {
                string sql = @"
                    SELECT NVL(lvl_no, 0) as LevelNo
                    FROM glcompm
                    WHERE TRIM(comp_id) = :cost_center_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("cost_center_id", OracleDbType.Char).Value = costCenterId?.Trim().ToUpper();

                    var result = cmd.ExecuteScalar();
                    return SafeGetInt(result);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCostCenterLevelNo: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Save role-to-cost-center associations with company (implements SAVE_REPROLECCT procedure logic)
        /// </summary>
        public void SaveRoleCostCenters(string roleId, string companyId, List<string> costCenterIds)
        {
            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    // First, delete existing role-cost center associations
                    DeleteExistingAssignments(conn, roleId);

                    // Then insert new assignments
                    if (costCenterIds != null && costCenterIds.Count > 0)
                    {
                        foreach (var costCenterId in costCenterIds)
                        {
                            if (!string.IsNullOrWhiteSpace(costCenterId))
                            {
                                SaveRoleCostCenter(conn, roleId, costCenterId);
                            }
                        }
                    }

                    // Update the role with company information
                    if (!string.IsNullOrWhiteSpace(companyId))
                    {
                        UpdateRoleCompany(conn, roleId, companyId);
                    }

                    Debug.WriteLine($"Successfully saved {costCenterIds?.Count ?? 0} cost center assignments for role: {roleId}, company: {companyId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveRoleCostCenters: {ex.Message}");
                throw new Exception($"Failed to save role cost centers: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete existing role-cost center assignments
        /// </summary>
        private void DeleteExistingAssignments(OracleConnection conn, string roleId)
        {
            try
            {
                string sql = @"
                    DELETE FROM rep_roles_cct
                    WHERE TRIM(roleid) = :role_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId?.Trim();

                    cmd.ExecuteNonQuery();
                    Debug.WriteLine($"Deleted existing assignments for role: {roleId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteExistingAssignments: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Save individual role-cost center assignment (implements SAVE_REPROLECCT procedure)
        /// </summary>
        private void SaveRoleCostCenter(OracleConnection conn, string roleId, string costCenterId)
        {
            try
            {
                // Get the level number for this cost center
                int lvlNo = GetCostCenterLevelNo(conn, costCenterId);

                // Check if assignment already exists
                if (DoesAssignmentExist(conn, roleId, costCenterId, lvlNo))
                {
                    // Update existing assignment
                    UpdateRoleCostCenter(conn, roleId, costCenterId, lvlNo);
                }
                else
                {
                    // Insert new assignment
                    InsertRoleCostCenter(conn, roleId, costCenterId, lvlNo);
                }

                Debug.WriteLine($"Saved cost center assignment - Role: {roleId}, CostCenter: {costCenterId}, Level: {lvlNo}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveRoleCostCenter: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check if role-cost center assignment exists
        /// </summary>
        private bool DoesAssignmentExist(OracleConnection conn, string roleId, string costCenterId, int lvlNo)
        {
            try
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM rep_roles_cct
                    WHERE TRIM(roleid) = :role_id
                      AND TRIM(costcentre) = :cost_centre_id
                      AND lvl_no = :lvl_no";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId?.Trim();
                    cmd.Parameters.Add("cost_centre_id", OracleDbType.Char).Value = costCenterId?.Trim().ToUpper();
                    cmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;

                    var result = cmd.ExecuteScalar();
                    return result != null && int.TryParse(result.ToString(), out var count) && count > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DoesAssignmentExist: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Insert new role-cost center assignment
        /// </summary>
        private void InsertRoleCostCenter(OracleConnection conn, string roleId, string costCenterId, int lvlNo)
        {
            try
            {
                string sql = @"
                    INSERT INTO rep_roles_cct (roleid, costcentre, lvl_no)
                    VALUES (:role_id, :cost_centre_id, :lvl_no)";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId?.Trim();
                    cmd.Parameters.Add("cost_centre_id", OracleDbType.Char).Value = costCenterId?.Trim().ToUpper();
                    cmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;

                    cmd.ExecuteNonQuery();
                    Debug.WriteLine($"Inserted cost center assignment for role: {roleId}, costcenter: {costCenterId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InsertRoleCostCenter: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update existing role-cost center assignment
        /// </summary>
        private void UpdateRoleCostCenter(OracleConnection conn, string roleId, string costCenterId, int lvlNo)
        {
            try
            {
                string sql = @"
                    UPDATE rep_roles_cct
                    SET roleid = :role_id,
                        costcentre = :cost_centre_id,
                        lvl_no = :lvl_no
                    WHERE TRIM(roleid) = :role_id
                      AND TRIM(costcentre) = :cost_centre_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId?.Trim();
                    cmd.Parameters.Add("cost_centre_id", OracleDbType.Char).Value = costCenterId?.Trim().ToUpper();
                    cmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;

                    cmd.ExecuteNonQuery();
                    Debug.WriteLine($"Updated cost center assignment for role: {roleId}, costcenter: {costCenterId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateRoleCostCenter: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to safely convert object to int
        /// </summary>
        private int SafeGetInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            if (int.TryParse(value.ToString(), out var result))
                return result;

            return 0;
        }

        /// <summary>
        /// Update role with company information
        /// </summary>
        private void UpdateRoleCompany(OracleConnection conn, string roleId, string companyId)
        {
            try
            {
                string sql = @"
                    UPDATE rep_role_new
                    SET company = :comp_id
                    WHERE TRIM(roleid) = :role_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId?.Trim();
                    cmd.Parameters.Add("comp_id", OracleDbType.Char).Value = companyId?.Trim().ToUpper();

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        Debug.WriteLine($"Warning: No rows updated for role {roleId} company");
                    }
                    else
                    {
                        Debug.WriteLine($"Updated company {companyId} for role {roleId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateRoleCompany: {ex.Message}");
                throw;
            }
        }
    }
}
