using MISReports_Api.DBAccess;
using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.Collection.ReceivablePosition
{
    /// <summary>
    /// Data access for the receive_position table.
    ///
    /// CONNECTION ROUTING:
    ///   bill_type = 'O'  →  InformixConnection     (GetConnection(false))
    ///   bill_type = 'B'  →  InformixBulkConnection (GetConnection(true))
    ///
    /// The table exists on BOTH databases, each holding its own bill_type rows.
    /// There is NO cross-DB fallback — if data doesn't exist for a given
    /// bill_cycle on the correct DB it simply isn't available yet.
    /// </summary>
    public class ReceivablePositionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // ── Connection routing ──────────────────────────────────────────────────

        /// <summary>Returns true when billType = 'B' (use bulk DB), false for 'O' (ordinary DB).</summary>
        private static bool IsBulk(string billType)
            => !string.IsNullOrWhiteSpace(billType)
               && billType.Trim().Equals("B", StringComparison.OrdinalIgnoreCase);

        // ── TestConnection ──────────────────────────────────────────────────────

        public bool TestConnection(out string errorMessage)
        {
            // Test ordinary connection by default (used for most health-check calls).
            // The CollectionController calls this before any query; at that point
            // billType is not yet known, so we verify the ordinary connection.
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public bool TestConnection(out string errorMessage, string billType)
        {
            return _dbConnection.TestConnection(out errorMessage, IsBulk(billType));
        }

        // ── Report ──────────────────────────────────────────────────────────────

        public List<ReceivablePositionModel> GetReceivablePositionReport(ReceivablePositionRequest request)
        {
            try
            {
                logger.Info("=== START GetReceivablePositionReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, BillType={request.BillType}, AreaCode={request.AreaCode}");

                bool bulk = IsBulk(request.BillType);
                logger.Info($"Using {(bulk ? "Bulk" : "Ordinary")} connection for bill_type='{request.BillType}'");

                using (var conn = _dbConnection.GetConnection(bulk))
                {
                    conn.Open();
                    var results = GetAreaReportData(conn, request);
                    logger.Info($"=== END GetReceivablePositionReport - {results.Count} records ===");
                    return results;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching receivable position report");
                throw;
            }
        }

        // ── Areas by Province ───────────────────────────────────────────────────

        public List<ReceivablePositionAreaModel> GetAreasByProvince(
            string provinceCode,
            string billType = null,
            string billCycle = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
                return new List<ReceivablePositionAreaModel>();

            bool bulk = IsBulk(billType);
            logger.Info($"GetAreasByProvince: province={provinceCode}, billType={billType}, billCycle={billCycle}, bulk={bulk}");

            return FetchAreasByProvince(provinceCode, billType, billCycle, bulk);
        }

        // ── Areas by Region ─────────────────────────────────────────────────────

        public List<ReceivablePositionAreaModel> GetAreasByRegion(
            string regionCode,
            string billType = null,
            string billCycle = null)
        {
            if (string.IsNullOrWhiteSpace(regionCode))
                return new List<ReceivablePositionAreaModel>();

            bool bulk = IsBulk(billType);
            logger.Info($"GetAreasByRegion: region={regionCode}, billType={billType}, billCycle={billCycle}, bulk={bulk}");

            return FetchAreasByRegion(regionCode, billType, billCycle, bulk);
        }

        // ── Max bill cycle ──────────────────────────────────────────────────────

        public int? GetMaxBillCycle(string billType = null)
        {
            bool bulk = IsBulk(billType);

            // Try filtered by bill_type first so each type gets its own correct max,
            // then fall back to unfiltered on the same DB, then mon_tot.
            return FirstAvailableMax(
                QueryMaxBillCycle(bulk, billType),
                QueryMaxBillCycle(bulk, null),
                QueryMaxBillCycleFromMonTot(bulk));
        }

        private static int? FirstAvailableMax(params int?[] values)
        {
            foreach (var v in values)
                if (v.HasValue) return v;
            return null;
        }

        private int? QueryMaxBillCycle(bool useBulk, string billType = null)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulk))
                {
                    conn.Open();

                    bool filter = !string.IsNullOrWhiteSpace(billType);
                    string sql = filter
                        ? "SELECT MAX(bill_cycle) FROM receive_position WHERE bill_type = ?"
                        : "SELECT MAX(bill_cycle) FROM receive_position";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (filter)
                            cmd.Parameters.AddWithValue("?", billType.Trim().ToUpper());

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value
                            && int.TryParse(result.ToString(), out int max))
                            return max;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read max bill_cycle from receive_position ({(useBulk ? "bulk" : "ordinary")} DB)");
            }
            return null;
        }

        private int? QueryMaxBillCycleFromMonTot(bool useBulk)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulk))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT MAX(bill_cycle) FROM mon_tot", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value
                            && int.TryParse(result.ToString(), out int max))
                            return max;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read max bill_cycle from mon_tot ({(useBulk ? "bulk" : "ordinary")} DB)");
            }
            return null;
        }

        // ── FetchAreasByProvince ────────────────────────────────────────────────

        private List<ReceivablePositionAreaModel> FetchAreasByProvince(
            string provinceCode,
            string billType,
            string billCycle,
            bool useBulk)
        {
            var provinceVariants = GetProvinceCodeVariants(provinceCode);

            using (var conn = _dbConnection.GetConnection(useBulk))
            {
                conn.Open();

                // When billCycle + billType are both provided, query receive_position
                // so only areas that ACTUALLY HAVE DATA for this cycle are returned.
                // If nothing found → return empty. Do NOT fall through to the plain
                // areas table; returning phantom areas causes the "Found N areas but
                // no records" error in the frontend.
                if (!string.IsNullOrWhiteSpace(billCycle) && !string.IsNullOrWhiteSpace(billType))
                {
                    foreach (var provCode in provinceVariants)
                    {
                        var rows = QueryAreaList(
                            conn,
                            @"SELECT DISTINCT a.area_code, a.area_name
                              FROM receive_position rp, areas a
                              WHERE rp.area_code = a.area_code
                                AND a.prov_code   = ?
                                AND rp.bill_cycle = ?
                                AND rp.bill_type  = ?
                              ORDER BY a.area_name",
                            provCode,
                            billCycle.Trim(),
                            billType.Trim().ToUpper());

                        if (rows.Count > 0) return rows;
                    }
                    // No data for this bill_cycle+bill_type combination on this DB
                    logger.Warn($"FetchAreasByProvince: no receive_position rows for " +
                                $"prov={provinceCode}, cycle={billCycle}, type={billType}, bulk={useBulk}");
                    return new List<ReceivablePositionAreaModel>();
                }

                // No cycle/type filter — return all areas for the province
                foreach (var provCode in provinceVariants)
                {
                    var rows = QueryAreaList(
                        conn,
                        @"SELECT area_code, area_name
                          FROM areas
                          WHERE prov_code = ?
                          ORDER BY area_name",
                        provCode);
                    if (rows.Count > 0) return rows;

                    var rowsJoin = QueryAreaList(
                        conn,
                        @"SELECT a.area_code, a.area_name
                          FROM areas a, provinces p
                          WHERE a.prov_code = p.prov_code
                            AND p.prov_code = ?
                          ORDER BY a.area_name",
                        provCode);
                    if (rowsJoin.Count > 0) return rowsJoin;
                }
            }

            return new List<ReceivablePositionAreaModel>();
        }

        // ── FetchAreasByRegion ──────────────────────────────────────────────────

        private List<ReceivablePositionAreaModel> FetchAreasByRegion(
            string regionCode,
            string billType,
            string billCycle,
            bool useBulk)
        {
            var region = regionCode.Trim();

            using (var conn = _dbConnection.GetConnection(useBulk))
            {
                conn.Open();

                if (!string.IsNullOrWhiteSpace(billCycle) && !string.IsNullOrWhiteSpace(billType))
                {
                    var rows = QueryAreaList(
                        conn,
                        @"SELECT DISTINCT a.area_code, a.area_name
                          FROM receive_position rp, areas a
                          WHERE rp.area_code = a.area_code
                            AND a.region      = ?
                            AND rp.bill_cycle = ?
                            AND rp.bill_type  = ?
                          ORDER BY a.area_name",
                        region,
                        billCycle.Trim(),
                        billType.Trim().ToUpper());

                    if (rows.Count > 0) return rows;

                    logger.Warn($"FetchAreasByRegion: no receive_position rows for " +
                                $"region={regionCode}, cycle={billCycle}, type={billType}, bulk={useBulk}");
                    return new List<ReceivablePositionAreaModel>();
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

        // ── QueryAreaList helper ────────────────────────────────────────────────

        private static List<ReceivablePositionAreaModel> QueryAreaList(
            OleDbConnection conn,
            string sql,
            params string[] parameters)
        {
            var results = new List<ReceivablePositionAreaModel>();
            using (var cmd = new OleDbCommand(sql, conn))
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue("?", p);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        results.Add(new ReceivablePositionAreaModel
                        {
                            AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                            AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim()
                        });
                }
            }
            return results;
        }

        // ── Province code variants ──────────────────────────────────────────────

        private static List<string> GetProvinceCodeVariants(string provinceCode)
        {
            var variants = new List<string>();
            var trimmed = provinceCode.Trim();
            if (string.IsNullOrEmpty(trimmed)) return variants;

            variants.Add(trimmed);
            if (char.IsDigit(trimmed[0]))
            {
                var padded = trimmed.PadLeft(2, '0');
                if (!variants.Contains(padded, StringComparer.OrdinalIgnoreCase))
                    variants.Add(padded);
            }
            return variants;
        }

        // ── Distinct bill types ─────────────────────────────────────────────────

        public List<string> GetDistinctBillTypes()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                logger.Info("=== START GetDistinctBillTypes ===");
                CollectBillTypes(results, false); // ordinary DB → 'O' rows
                CollectBillTypes(results, true);  // bulk DB     → 'B' rows
                logger.Info($"Retrieved {results.Count} distinct bill types");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching distinct bill types");
                throw;
            }
            return results.OrderBy(t => t).ToList();
        }

        private void CollectBillTypes(HashSet<string> results, bool useBulk)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(useBulk))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(
                        "SELECT DISTINCT bill_type FROM receive_position ORDER BY bill_type", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            if (!reader.IsDBNull(0))
                                results.Add(reader.GetString(0).Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read bill types from {(useBulk ? "bulk" : "ordinary")} DB");
            }
        }

        // ── Report data ─────────────────────────────────────────────────────────

        private List<ReceivablePositionModel> GetAreaReportData(
            OleDbConnection conn,
            ReceivablePositionRequest request)
        {
            var results = new List<ReceivablePositionModel>();
            try
            {
                const string sql = @"
                    SELECT rp.area_code,
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
                      AND rp.bill_type  = ?
                    ORDER BY rp.area_code";

                logger.Info($"SQL params: area={request.AreaCode}, cycle={request.BillCycle}, type={request.BillType}");

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
                logger.Info($"Retrieved {results.Count} records");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Receivable Position report data");
                throw;
            }
            return results;
        }

        private ReceivablePositionModel MapReaderToModel(OleDbDataReader reader)
        {
            var m = new ReceivablePositionModel();
            try
            {
                m.AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();
                m.AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();

                m.OpeningBalance = Fmt(reader, 2);
                m.MonthlyCharge = Fmt(reader, 3);
                m.Debits = Fmt(reader, 4);
                m.Credits = Fmt(reader, 5);
                m.UnderCharge = Fmt(reader, 6);
                m.OverCharge = Fmt(reader, 7);
                m.Payments = Fmt(reader, 8);
                m.ClosingBalance = Fmt(reader, 9);
                m.ClosingBalanceWithoutFinAcc = Fmt(reader, 10);
                m.AverageCharge = Fmt(reader, 11);
                m.NoOfMonthsInArrears = Fmt(reader, 12);
                m.NoOfMonthsInArrearsWithoutFinAcc = Fmt(reader, 13);

                m.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping reader to ReceivablePositionModel");
                m.ErrorMessage = ex.Message;
            }
            return m;
        }

        private string Fmt(OleDbDataReader reader, int ordinal)
        {
            try
            {
                return reader.IsDBNull(ordinal)
                    ? "0"
                    : Convert.ToDecimal(reader.GetValue(ordinal)).ToString("###,###.##");
            }
            catch { return "0"; }
        }
    }
}