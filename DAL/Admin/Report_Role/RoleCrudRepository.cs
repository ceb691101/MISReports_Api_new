using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace MISReports_Api.DAL
{
    public class RoleCrudRepository
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["OracleTest"].ConnectionString;

        private OracleConnection OpenConnection()
        {
            var conn = new OracleConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // 1) SELECT - Check if user exists (Normal User)
        public bool ExistsNormalUser(string roleId)
        {
            const string sql = @"
                SELECT roleid, rolename, usertype
                FROM rep_role_new
                WHERE roleid = :roleID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return false;
                    var userType = reader["USERTYPE"]?.ToString()?.Trim();
                    return string.Equals(userType, "User", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // 1) SELECT - Check if admin exists
        public bool ExistsAdmin(string roleId)
        {
            const string sql = @"
                SELECT roleid, rolename
                FROM rep_role_new
                WHERE roleid = :roleID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return false;
                    return true;
                }
            }
        }

        // 1) SELECT - Inside DELETE_ADMINUSER
        public string GetUserType(string roleId)
        {
            const string sql = @"
                SELECT usertype
                FROM rep_role_new
                WHERE roleid = :roleID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        // 1) SELECT - Inside SAVE_ADMINUSER
        public string GetRoleName(string roleId)
        {
            const string sql = @"
                SELECT rolename
                FROM rep_role_new
                WHERE roleid = :roleID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        // 1) SELECT - Inside SAVE_REPROLECCT
        public bool ExistsRoleCostCentre(string roleId, string cct)
        {
            const string sql = @"
                SELECT costcentre, lvl_no
                FROM rep_roles_cct_new
                WHERE roleid = :roleID
                AND costcentre = :cct";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("cct", OracleDbType.Varchar2).Value = cct?.Trim();

                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }

        // 2) INSERT - Insert new user
        public int InsertUser(string roleId, string roleName, string pwd, string userType, string company, string compSub)
        {
            const string sql = @"
                INSERT INTO rep_role_new 
                (roleid, rolename, password, usertype, company, comp_dup)
                VALUES 
                (:roleID, :roleName, :pwd, :userType, :company, :compSub)";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("roleName", OracleDbType.Varchar2).Value = roleName?.Trim();
                cmd.Parameters.Add("pwd", OracleDbType.Varchar2).Value = pwd?.Trim();
                cmd.Parameters.Add("userType", OracleDbType.Varchar2).Value = userType?.Trim();
                cmd.Parameters.Add("company", OracleDbType.Varchar2).Value = company?.Trim();
                cmd.Parameters.Add("compSub", OracleDbType.Varchar2).Value = compSub?.Trim();

                return cmd.ExecuteNonQuery();
            }
        }

        // 2) INSERT - Insert user cost centre
        public int InsertUserCostCentre(string roleId, string cct, int lvlNo)
        {
            const string sql = @"
                INSERT INTO rep_roles_cct_new 
                (roleid, costcentre, lvl_no)
                VALUES 
                (:roleID, :cct, :lvlNo)";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("cct", OracleDbType.Varchar2).Value = cct?.Trim();
                cmd.Parameters.Add("lvlNo", OracleDbType.Int32).Value = lvlNo;

                return cmd.ExecuteNonQuery();
            }
        }

        // 3) UPDATE - Update user
        public int UpdateUser(string roleId, string roleName, string pwd, string userType, string company, string compSub)
        {
            const string sql = @"
                UPDATE rep_role_new
                SET 
                    rolename = :roleName,
                    password = :pwd,
                    usertype = :userType,
                    company  = :company,
                    comp_dup = :compSub
                WHERE roleid = :roleID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleName", OracleDbType.Varchar2).Value = roleName?.Trim();
                cmd.Parameters.Add("pwd", OracleDbType.Varchar2).Value = pwd?.Trim();
                cmd.Parameters.Add("userType", OracleDbType.Varchar2).Value = userType?.Trim();
                cmd.Parameters.Add("company", OracleDbType.Varchar2).Value = company?.Trim();
                cmd.Parameters.Add("compSub", OracleDbType.Varchar2).Value = compSub?.Trim();
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();

                return cmd.ExecuteNonQuery();
            }
        }

        // 3) UPDATE - Update cost centre level
        public int UpdateCostCentreLevel(string roleId, string cct, int lvlNo)
        {
            const string sql = @"
                UPDATE rep_roles_cct_new
                SET lvl_no = :lvlNo
                WHERE roleid = :roleID
                AND costcentre = :cct";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("lvlNo", OracleDbType.Int32).Value = lvlNo;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("cct", OracleDbType.Varchar2).Value = cct?.Trim();

                return cmd.ExecuteNonQuery();
            }
        }

        // 4) DELETE - basic
        public int DeleteUserBasic(string roleId)
        {
            const string sql = @"
                DELETE FROM rep_role_new
                WHERE roleid = :roleID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                return cmd.ExecuteNonQuery();
            }
        }

        // 4) DELETE - by type
        public int DeleteUserByType(string roleId, string type) // User / Administrator
        {
            const string sql = @"
                DELETE FROM rep_role_new
                WHERE roleid = :roleID AND usertype = :type";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("type", OracleDbType.Varchar2).Value = type?.Trim();
                return cmd.ExecuteNonQuery();
            }
        }

        // 4) DELETE - all reports for user
        public int DeleteAllReports(string roleId)
        {
            const string sql = @"
                DELETE FROM rep_roles_rep_new
                WHERE roleid = :roleID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                return cmd.ExecuteNonQuery();
            }
        }

        // 4) DELETE - reports by category
        public int DeleteReportsByCategory(string roleId, IEnumerable<string> catCodes)
        {
            var codes = (catCodes ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (codes.Count == 0) return 0;

            var bindNames = new List<string>();
            for (int i = 0; i < codes.Count; i++) bindNames.Add($":cat{i}");

            var sql = $@"
                DELETE FROM rep_roles_rep_new
                WHERE roleid = :roleID
                AND catcode IN ({string.Join(",", bindNames)})";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();

                for (int i = 0; i < codes.Count; i++)
                {
                    cmd.Parameters.Add($"cat{i}", OracleDbType.Varchar2).Value = codes[i];
                }

                return cmd.ExecuteNonQuery();
            }
        }

        // 4) DELETE - specific report
        public int DeleteSpecificReport(string roleId, string repId)
        {
            const string sql = @"
                DELETE FROM rep_roles_rep_new
                WHERE roleid = :roleID
                AND repid = :repID";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("repID", OracleDbType.Varchar2).Value = repId?.Trim();
                return cmd.ExecuteNonQuery();
            }
        }

        // 4) DELETE - cost centre
        public int DeleteCostCentre(string roleId, string cct)
        {
            const string sql = @"
                DELETE FROM rep_roles_cct_new
                WHERE roleid = :roleID
                AND costcentre = :cct";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleID", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("cct", OracleDbType.Varchar2).Value = cct?.Trim();
                return cmd.ExecuteNonQuery();
            }
        }
    }
}