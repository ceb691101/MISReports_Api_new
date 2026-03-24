using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.General.SecurityDepositContractDemandBulk
{
    public class ContractDemandAreaProvinceDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true);
        }

        // ── GET ALL AREAS ──────────────────────────────────────────────────────
        // Reads every row from the `areas` table.
        // Returns: List<AreaModel> ordered by area_code
        public List<AreaModel> GetAllAreas()
        {
            var results = new List<AreaModel>();
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    string sql = "SELECT area_code, area_name FROM areas ORDER BY area_code";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new AreaModel
                            {
                                AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                                AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                            });
                        }
                    }
                }
                logger.Info($"GetAllAreas: retrieved {results.Count} areas");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching all areas");
                throw;
            }
            return results;
        }

        // ── GET ALL PROVINCES ──────────────────────────────────────────────────
        // Reads every row from the `provinces` table.
        // Returns: List<ProvinceModel> ordered by prov_code
        public List<ProvinceModel> GetAllProvinces()
        {
            var results = new List<ProvinceModel>();
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    string sql = "SELECT prov_code, prov_name FROM provinces ORDER BY prov_code";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ProvinceModel
                            {
                                ProvinceCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                                ProvinceName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                            });
                        }
                    }
                }
                logger.Info($"GetAllProvinces: retrieved {results.Count} provinces");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching all provinces");
                throw;
            }
            return results;
        }
    }
}