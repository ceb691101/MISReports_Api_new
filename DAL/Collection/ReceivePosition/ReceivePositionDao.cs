using MISReports_Api.DBAccess;
using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.Collection.ReceivablePosition
{
    public class ReceivablePositionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // bulk connection
        }

        public List<ReceivablePositionModel> GetReceivablePositionReport(ReceivablePositionRequest request)
        {
            try
            {
                logger.Info("=== START GetReceivablePositionReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, BillType={request.BillType}, AreaCode={request.AreaCode}");

                bool useBulk = UseBulkConnection(request.BillType);
                var results = FetchReceivablePositionReport(request, useBulk);

                if (results.Count == 0)
                    results = FetchReceivablePositionReport(request, !useBulk);

                logger.Info($"=== END GetReceivablePositionReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching receivable position report");
                throw;
            }
        }

        private List<ReceivablePositionModel> FetchReceivablePositionReport(
            ReceivablePositionRequest request,
            bool useBulkConnection)
        {
            using (var conn = _dbConnection.GetConnection(useBulkConnection))
            {
                conn.Open();
                return GetAreaReportData(conn, request);
            }
        }

        public List<ReceivablePositionAreaModel> GetAreasByProvince(
            string provinceCode,
            string billType = null,
            string billCycle = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
                return new List<ReceivablePositionAreaModel>();

            bool useBulk = UseBulkConnection(billType);
            var results = FetchAreasByProvince(provinceCode, billType, billCycle, useBulk);

            if (results.Count == 0)
                results = FetchAreasByProvince(provinceCode, billType, billCycle, !useBulk);

            return results;
        }

        public List<ReceivablePositionAreaModel> GetAreasByRegion(
            string regionCode,
            string billType = null,
            string billCycle = null)
        {
            if (string.IsNullOrWhiteSpace(regionCode))
                return new List<ReceivablePositionAreaModel>();

            bool useBulk = UseBulkConnection(billType);
            var results = FetchAreasByRegion(regionCode, billType, billCycle, useBulk);

            if (results.Count == 0)
                results = FetchAreasByRegion(regionCode, billType, billCycle, !useBulk);

            return results;
        }

        public int? GetMaxBillCycle(string billType = null)
        {
            if (string.IsNullOrWhiteSpace(billType))
            {
                return FirstAvailableMax(
                    QueryMaxBillCycle(false),
                    QueryMaxBillCycle(true),
                    QueryMaxBillCycleFromMonTot(true));
            }

            if (UseBulkConnection(billType))
            {
                return FirstAvailableMax(
                    QueryMaxBillCycle(true),
                    QueryMaxBillCycle(false),
                    QueryMaxBillCycleFromMonTot(true));
            }

            return FirstAvailableMax(
                QueryMaxBillCycle(false),
                QueryMaxBillCycle(true));
        }

        private static int? FirstAvailableMax(params int?[] values)
        {
            foreach (var value in values)
            {
                if (value.HasValue)
                    return value;
            }

            return null;
        }

        private int? QueryMaxBillCycleFromMonTot(bool useBulkConnection)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulkConnection))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand("SELECT MAX(bill_cycle) FROM mon_tot", conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value
                            && int.TryParse(maxCycleObj.ToString(), out int maxCycle))
                        {
                            return maxCycle;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read max bill cycle from mon_tot ({(useBulkConnection ? "bulk" : "ordinary")})");
            }

            return null;
        }

        private int? QueryMaxBillCycle(bool useBulkConnection)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulkConnection))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand("SELECT MAX(bill_cycle) FROM receive_position", conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value
                            && int.TryParse(maxCycleObj.ToString(), out int maxCycle))
                        {
                            return maxCycle;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read max bill cycle from {(useBulkConnection ? "bulk" : "ordinary")} DB");
            }

            return null;
        }

        private List<ReceivablePositionAreaModel> FetchAreasByProvince(
            string provinceCode,
            string billType,
            string billCycle,
            bool useBulkConnection)
        {
            var provinceVariants = GetProvinceCodeVariants(provinceCode);

            using (var conn = _dbConnection.GetConnection(useBulkConnection))
            {
                conn.Open();

                if (!string.IsNullOrWhiteSpace(billCycle) && !string.IsNullOrWhiteSpace(billType))
                {
                    foreach (var provCode in provinceVariants)
                    {
                        var fromReceivePosition = QueryAreaList(
                            conn,
                            @"SELECT DISTINCT a.area_code, a.area_name
                              FROM receive_position rp, areas a
                              WHERE rp.area_code = a.area_code
                                AND a.prov_code = ?
                                AND rp.bill_cycle = ?
                                AND rp.bill_type = ?
                              ORDER BY a.area_name",
                            provCode,
                            billCycle.Trim(),
                            billType.Trim().ToUpper());

                        if (fromReceivePosition.Count > 0)
                            return fromReceivePosition;
                    }
                }

                foreach (var provCode in provinceVariants)
                {
                    var fromAreas = QueryAreaList(
                        conn,
                        @"SELECT area_code, area_name
                          FROM areas
                          WHERE prov_code = ?
                          ORDER BY area_name",
                        provCode);

                    if (fromAreas.Count > 0)
                        return fromAreas;

                    var fromProvinceJoin = QueryAreaList(
                        conn,
                        @"SELECT a.area_code, a.area_name
                          FROM areas a, provinces p
                          WHERE a.prov_code = p.prov_code
                            AND p.prov_code = ?
                          ORDER BY a.area_name",
                        provCode);

                    if (fromProvinceJoin.Count > 0)
                        return fromProvinceJoin;
                }
            }

            return new List<ReceivablePositionAreaModel>();
        }

        private List<ReceivablePositionAreaModel> FetchAreasByRegion(
            string regionCode,
            string billType,
            string billCycle,
            bool useBulkConnection)
        {
            var region = regionCode.Trim();

            using (var conn = _dbConnection.GetConnection(useBulkConnection))
            {
                conn.Open();

                if (!string.IsNullOrWhiteSpace(billCycle) && !string.IsNullOrWhiteSpace(billType))
                {
                    var fromReceivePosition = QueryAreaList(
                        conn,
                        @"SELECT DISTINCT a.area_code, a.area_name
                          FROM receive_position rp, areas a
                          WHERE rp.area_code = a.area_code
                            AND a.region = ?
                            AND rp.bill_cycle = ?
                            AND rp.bill_type = ?
                          ORDER BY a.area_name",
                        region,
                        billCycle.Trim(),
                        billType.Trim().ToUpper());

                    if (fromReceivePosition.Count > 0)
                        return fromReceivePosition;
                }

                return QueryAreaList(
                    conn,
                    @"SELECT area_code, area_name
                      FROM areas
                      WHERE region = ?
                      ORDER BY area_name",
                    region);
            }
        }

        private static List<ReceivablePositionAreaModel> QueryAreaList(
            OleDbConnection conn,
            string sql,
            params string[] parameters)
        {
            var results = new List<ReceivablePositionAreaModel>();

            using (var cmd = new OleDbCommand(sql, conn))
            {
                foreach (var value in parameters)
                    cmd.Parameters.AddWithValue("?", value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ReceivablePositionAreaModel
                        {
                            AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                            AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim()
                        });
                    }
                }
            }

            return results;
        }

        private static List<string> GetProvinceCodeVariants(string provinceCode)
        {
            var variants = new List<string>();
            var trimmed = provinceCode.Trim();

            if (string.IsNullOrEmpty(trimmed))
                return variants;

            variants.Add(trimmed);

            if (char.IsDigit(trimmed[0]))
            {
                var padded = trimmed.PadLeft(2, '0');
                if (!variants.Contains(padded, StringComparer.OrdinalIgnoreCase))
                    variants.Add(padded);
            }

            return variants;
        }

        private static bool UseBulkConnection(string billType)
        {
            return !string.IsNullOrWhiteSpace(billType)
                && billType.Trim().Equals("B", StringComparison.OrdinalIgnoreCase);
        }

        public List<string> GetDistinctBillTypes()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                logger.Info("=== START GetDistinctBillTypes ===");
                CollectDistinctBillTypes(results, false);
                CollectDistinctBillTypes(results, true);
                logger.Info($"Retrieved {results.Count} distinct bill types");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching distinct bill types");
                throw;
            }

            return results.OrderBy(t => t).ToList();
        }

        private void CollectDistinctBillTypes(HashSet<string> results, bool useBulkConnection)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulkConnection))
                {
                    conn.Open();

                    string sql = "SELECT DISTINCT bill_type FROM receive_position ORDER BY bill_type";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                results.Add(reader.GetString(0).Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read bill types from {(useBulkConnection ? "bulk" : "ordinary")} DB");
            }
        }

        /// <summary>
        /// Gets receivable position data for a given area code, bill cycle, and bill type.
        /// SQL pattern:
        ///   SELECT * FROM receive_position WHERE area_code = ? AND bill_cycle = ? AND bill_type = ?
        /// </summary>
        private List<ReceivablePositionModel> GetAreaReportData(OleDbConnection conn, ReceivablePositionRequest request)
        {
            var results = new List<ReceivablePositionModel>();

            try
            {
                string sql = @"SELECT rp.area_code,
                                      a.area_name,
                                      rp.op_bal,
                                      rp.mon_chg,
                                      rp.debits,
                                      rp.credits,
                                      rp.un_chg,
                                      rp.ov_chg,
                                      rp.payments,
                                      rp.close_bal,
                                      rp.cl_bal_fin,
                                      rp.avg_3,
                                      rp.no_months,
                                      rp.mon_wofin
                               FROM receive_position rp
                               LEFT JOIN areas a ON rp.area_code = a.area_code
                               WHERE rp.area_code = ?
                                 AND rp.bill_cycle = ?
                                 AND rp.bill_type = ?
                               ORDER BY rp.area_code";

                logger.Info($"Executing SQL: AreaCode={request.AreaCode}, BillCycle={request.BillCycle}, BillType={request.BillType}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", request.AreaCode);
                    cmd.Parameters.AddWithValue("?", request.BillCycle);
                    cmd.Parameters.AddWithValue("?", request.BillType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);
                            model.BillCycle = request.BillCycle;
                            model.BillType = request.BillType;
                            results.Add(model);
                        }
                    }
                }

                logger.Info($"Retrieved {results.Count} records for Receivable Position report");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Receivable Position report data");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Maps database reader to ReceivablePositionModel using ordinal positions.
        /// Columns 0-1: string fields. Columns 2-13: numeric fields.
        /// </summary>
        private ReceivablePositionModel MapReaderToModel(OleDbDataReader reader)
        {
            var model = new ReceivablePositionModel();

            try
            {
                // String fields (ordinals 0-1)
                model.AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();
                model.AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();

                // Numeric fields (ordinals 2-13) — read and format directly
                model.OpeningBalance = FormatDecimal(reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2)));
                model.MonthlyCharge = FormatDecimal(reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3)));
                model.Debits = FormatDecimal(reader.IsDBNull(4) ? 0 : Convert.ToDecimal(reader.GetValue(4)));
                model.Credits = FormatDecimal(reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)));
                model.UnderCharge = FormatDecimal(reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6)));
                model.OverCharge = FormatDecimal(reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)));
                model.Payments = FormatDecimal(reader.IsDBNull(8) ? 0 : Convert.ToDecimal(reader.GetValue(8)));
                model.ClosingBalance = FormatDecimal(reader.IsDBNull(9) ? 0 : Convert.ToDecimal(reader.GetValue(9)));
                model.ClosingBalanceWithoutFinAcc = FormatDecimal(reader.IsDBNull(10) ? 0 : Convert.ToDecimal(reader.GetValue(10)));
                model.AverageCharge = FormatDecimal(reader.IsDBNull(11) ? 0 : Convert.ToDecimal(reader.GetValue(11)));
                model.NoOfMonthsInArrears = FormatDecimal(reader.IsDBNull(12) ? 0 : Convert.ToDecimal(reader.GetValue(12)));
                model.NoOfMonthsInArrearsWithoutFinAcc = FormatDecimal(reader.IsDBNull(13) ? 0 : Convert.ToDecimal(reader.GetValue(13)));

                model.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping reader to ReceivablePositionModel");
                model.ErrorMessage = ex.Message;
            }

            return model;
        }

        private string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return null;
                return reader.GetString(ordinal).Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return null;
            }
        }

        private string FormatDecimal(decimal value)
        {
            try
            {
                return value.ToString("###,###.##");
            }
            catch
            {
                return "0.00";
            }
        }

        private string FormatInteger(decimal value)
        {
            try
            {
                return ((int)value).ToString("###,###");
            }
            catch
            {
                return "0";
            }
        }
    }
}