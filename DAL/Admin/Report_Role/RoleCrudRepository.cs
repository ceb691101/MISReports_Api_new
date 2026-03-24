using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace MISReports_Api.DAL
{
	public class RoleCrudRepository
	{
		private readonly string connectionString = ConfigurationManager.ConnectionStrings["OracleTest"].ConnectionString;

		private static string Normalize(string value)
		{
			return value?.Trim();
		}

		private static List<string> NormalizeDistinctCodes(IEnumerable<string> codes)
		{
			return (codes ?? Enumerable.Empty<string>())
				.Where(code => !string.IsNullOrWhiteSpace(code))
				.Select(code => code.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public List<RoleReportLookupDto> GetReportsByCategory(List<string> catCodes)
		{
			var normalizedCodes = NormalizeDistinctCodes(catCodes);
			var result = new List<RoleReportLookupDto>();

			if (normalizedCodes.Count == 0)
			{
				return result;
			}

			try
			{
				using (var conn = new OracleConnection(connectionString))
				{
					conn.Open();

					var bindNames = new List<string>();
					for (var i = 0; i < normalizedCodes.Count; i++)
					{
						bindNames.Add(":cat" + i);
					}

					var sql = @"
SELECT CATCODE, REPID
FROM REP_REPORTS_NEW
WHERE CATCODE IN (" + string.Join(",", bindNames) + @")
AND FAVORITE = '1'
AND ACTIVE = '1'";

					using (var cmd = new OracleCommand(sql, conn))
					{
						cmd.BindByName = true;
						for (var i = 0; i < normalizedCodes.Count; i++)
						{
							cmd.Parameters.Add("cat" + i, OracleDbType.Varchar2).Value = normalizedCodes[i];
						}

						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								result.Add(new RoleReportLookupDto
								{
									CatCode = reader["CATCODE"]?.ToString()?.Trim(),
									RepId = reader["REPID"]?.ToString()?.Trim(),
									RepName = string.Empty
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error in GetReportsByCategory: " + ex.Message);
				throw;
			}

			return result;
		}

		public bool ExistsRoleReportExact(string roleId, string repId, string catCode)
		{
			const string sql = @"
SELECT CATCODE, REPID
FROM REP_ROLES_REP_NEW
WHERE ROLEID = :ROLEID
AND REPID = :REPID
AND CATCODE = :CATCODE";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = Normalize(repId);
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = Normalize(catCode);

					using (var reader = cmd.ExecuteReader())
					{
						return reader.Read();
					}
				}
			}
		}

		private bool ExistsRoleReportByRepId(string roleId, string repId)
		{
			const string sql = @"
SELECT 1
FROM REP_ROLES_REP_NEW
WHERE ROLEID = :ROLEID
AND REPID = :REPID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = Normalize(repId);
					using (var reader = cmd.ExecuteReader())
					{
						return reader.Read();
					}
				}
			}
		}

		public int InsertUserReport(string roleId, string catCode, string repId)
		{
			const string sql = @"
INSERT INTO REP_ROLES_REP_NEW
(REPID_NO, ROLEID, CATCODE, REPID, FAVORITE)
VALUES (SEQ_REP_ROLES_REP.NEXTVAL, :ROLEID, :CATCODE, :REPID, '1')";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = Normalize(catCode);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = Normalize(repId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public int UpdateUserReport(string roleId, string catCode, string repId)
		{
			const string sql = @"
UPDATE REP_ROLES_REP_NEW
SET CATCODE = :CATCODE,
	REPID   = :REPID
WHERE ROLEID = :ROLEID
AND REPID = :REPID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = Normalize(catCode);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = Normalize(repId);
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public RoleSaveResultDto SaveUserRoleReports(string roleId, List<RoleReportItemRequest> reports)
		{
			var result = new RoleSaveResultDto();

			if (string.IsNullOrWhiteSpace(roleId) || reports == null || reports.Count == 0)
			{
				return result;
			}

			foreach (var item in reports)
			{
				if (item == null || string.IsNullOrWhiteSpace(item.RepId) || string.IsNullOrWhiteSpace(item.CatCode))
				{
					result.Ignored++;
					continue;
				}

				var role = Normalize(roleId);
				var rep = Normalize(item.RepId);
				var cat = Normalize(item.CatCode);

				if (ExistsRoleReportExact(role, rep, cat))
				{
					// Keep servlet-compatible behavior: update when existing.
					UpdateUserReport(role, cat, rep);
					result.Updated++;
					continue;
				}

				if (ExistsRoleReportByRepId(role, rep))
				{
					UpdateUserReport(role, cat, rep);
					result.Updated++;
					continue;
				}

				InsertUserReport(role, cat, rep);
				result.Inserted++;
			}

			return result;
		}

		public int DeleteAllReports(string roleId)
		{
			const string sql = @"
DELETE FROM REP_ROLES_REP_NEW
WHERE ROLEID = :ROLEID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public int DeleteReportsByCategory(string roleId, string catCode)
		{
			const string sql = @"
DELETE FROM REP_ROLES_REP_NEW
WHERE ROLEID = :ROLEID
AND CATCODE = :CATCODE";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = Normalize(catCode);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public int DeleteReportByName(string roleId, string repId)
		{
			const string sql = @"
DELETE FROM REP_ROLES_REP_NEW
WHERE ROLEID = :ROLEID
AND REPID = :REPID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = Normalize(repId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public List<RoleReportLookupDto> GetReportsByName(string repId)
		{
			const string sql = @"
SELECT CATCODE, REPID, REPNAME
FROM REP_REPORTS_NEW
WHERE REPID = :REPID";

			var result = new List<RoleReportLookupDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = Normalize(repId);

					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							result.Add(new RoleReportLookupDto
							{
								CatCode = reader["CATCODE"]?.ToString()?.Trim(),
								RepId = reader["REPID"]?.ToString()?.Trim(),
								RepName = reader["REPNAME"]?.ToString()?.Trim()
							});
						}
					}
				}
			}

			return result;
		}

		public List<RoleCategoryDto> GetAllCategories()
		{
			const string sql = @"
SELECT CATCODE, CATNAME
FROM REP_CATS_NEW";

			var result = new List<RoleCategoryDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						result.Add(new RoleCategoryDto
						{
							CatCode = reader["CATCODE"]?.ToString()?.Trim(),
							CatName = reader["CATNAME"]?.ToString()?.Trim()
						});
					}
				}
			}

			return result;
		}

		public List<RoleReportLookupDto> GetAllActiveReports()
		{
			const string sql = @"
SELECT CATCODE, REPID, REPNAME
FROM REP_REPORTS_NEW
WHERE ACTIVE = '1'";

			var result = new List<RoleReportLookupDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						result.Add(new RoleReportLookupDto
						{
							CatCode = reader["CATCODE"]?.ToString()?.Trim(),
							RepId = reader["REPID"]?.ToString()?.Trim(),
							RepName = reader["REPNAME"]?.ToString()?.Trim()
						});
					}
				}
			}

			return result;
		}

		public List<RoleAssignedReportDto> GetUserAssignedReports(string roleId)
		{
			const string sql = @"
SELECT ROLEID, CATCODE, REPID
FROM REP_ROLES_REP_NEW
WHERE ROLEID = :ROLEID";

			var result = new List<RoleAssignedReportDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = Normalize(roleId);

					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							result.Add(new RoleAssignedReportDto
							{
								RoleId = reader["ROLEID"]?.ToString()?.Trim(),
								CatCode = reader["CATCODE"]?.ToString()?.Trim(),
								RepId = reader["REPID"]?.ToString()?.Trim()
							});
						}
					}
				}
			}

			return result;
		}
	}
}
