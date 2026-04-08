using System;
using System.Globalization;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.Models.Dashboard;

// ─────────────────────────────────────────────────────────────────────────────
// All dashboard controllers live in this single file. - all controllers in dashboard
// They cannot be merged into one class because each has a different RoutePrefix.
// ─────────────────────────────────────────────────────────────────────────────

namespace MISReports_Api.Controllers.Dashboard
{
    // =========================================================================
    // 1. MAIN DASHBOARD CONTROLLER
    //    Routes: api/dashboard/...
    // =========================================================================
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        private readonly BulkCustomersDao _bulkCustomersDao = new BulkCustomersDao();
        private readonly SalesAndCollectionRangeDao _salesAndCollectionRangeDao = new SalesAndCollectionRangeDao();
        private readonly OrdinaryCustomersDao _ordinaryCustomersDao = new OrdinaryCustomersDao();
        private readonly KioskCollectionDao _kioskCollectionDao = new KioskCollectionDao();

        /// <summary>GET api/dashboard/customers/active-count</summary>
        [HttpGet]
        [Route("customers/active-count")]
        public IHttpActionResult GetActiveCustomerCount()
        {
            try
            {
                if (!_bulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                int count = _bulkCustomersDao.GetActiveCustomerCount();

                return Ok(new
                {
                    data = new { activeCustomerCount = count },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get active customer count.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/ordinary-customers-summary?billCycle=0</summary>
        [HttpGet]
        [Route("ordinary-customers-summary")]
        public IHttpActionResult GetOrdinaryCustomersSummary([FromUri] string billCycle)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return Ok(new { data = (object)null, errorMessage = "Bill cycle is required." });

            try
            {
                if (!_ordinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _ordinaryCustomersDao.GetOrdinaryCustomersCount(billCycle);

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving ordinary customers summary.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/salesCollection/range/ordinary</summary>
        [HttpGet]
        [Route("salesCollection/range/ordinary")]
        public IHttpActionResult GetOrdinarySalesAndCollection()
        {
            try
            {
                if (!_salesAndCollectionRangeDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                SalesAndCollectionRangeResult result = _salesAndCollectionRangeDao.GetSalesAndCollectionRange();

                return Ok(new
                {
                    data = new
                    {
                        records = result.OrdinaryData.Select(rec => new
                        {
                            Date = ResolveSalesCollectionDate(rec),
                            Amount = rec.Collection,
                            rec.ErrorMessage
                        })
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get ordinary sales and collection data.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/salesCollection/range/bulk</summary>
        [HttpGet]
        [Route("salesCollection/range/bulk")]
        public IHttpActionResult GetBulkSalesAndCollection()
        {
            try
            {
                if (!_salesAndCollectionRangeDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                SalesAndCollectionRangeResult result = _salesAndCollectionRangeDao.GetSalesAndCollectionRange();

                return Ok(new
                {
                    data = new
                    {
                        records = result.BulkData.Select(rec => new
                        {
                            Date = ResolveSalesCollectionDate(rec),
                            Amount = rec.Collection,
                            rec.ErrorMessage
                        })
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get bulk sales and collection data.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/kiosk-collection?userId=KIOS00&fromDate=yyyy-MM-dd&toDate=yyyy-MM-dd</summary>
        [HttpGet]
        [Route("kiosk-collection")]
        public IHttpActionResult GetKioskCollection([FromUri] string userId = null, [FromUri] string fromDate = null, [FromUri] string toDate = null)
        {
            try
            {
                if (!_kioskCollectionDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                if (string.IsNullOrWhiteSpace(userId))
                    return Ok(new { data = (object)null, errorMessage = "userId is required." });

                string resolvedUserId = userId.Trim();

                DateTime resolvedToDate = DateTime.Today.AddDays(-1);
                if (!string.IsNullOrWhiteSpace(toDate) &&
                    !DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out resolvedToDate))
                {
                    return Ok(new { data = (object)null, errorMessage = "Invalid toDate format. Use yyyy-MM-dd." });
                }

                DateTime resolvedFromDate = resolvedToDate.AddDays(-7);
                if (!string.IsNullOrWhiteSpace(fromDate) &&
                    !DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out resolvedFromDate))
                {
                    return Ok(new { data = (object)null, errorMessage = "Invalid fromDate format. Use yyyy-MM-dd." });
                }

                if (resolvedFromDate > resolvedToDate)
                    return Ok(new { data = (object)null, errorMessage = "fromDate cannot be greater than toDate." });

                var records = _kioskCollectionDao.GetKioskCollection(resolvedUserId, resolvedFromDate, resolvedToDate);

                return Ok(new
                {
                    data = new
                    {
                        userId = resolvedUserId,
                        fromDate = resolvedFromDate.ToString("yyyy-MM-dd"),
                        toDate = resolvedToDate.ToString("yyyy-MM-dd"),
                        records
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get kiosk collection data.", errorDetails = ex.Message });
            }
        }

        private static string ResolveSalesCollectionDate(SalesAndCollectionModel record)
        {
            if (!string.IsNullOrWhiteSpace(record.Date))
                return record.Date;

            return FormatSalesCollectionDate(record.BillCycle);
        }

        private static string FormatSalesCollectionDate(int billCycle)
        {
            var raw = billCycle.ToString(CultureInfo.InvariantCulture);

            if (raw.Length == 8 && DateTime.TryParseExact(raw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return string.Empty;
        }
    }


    // SOLAR ORDINARY CUSTOMERS CONTROLLER
    //    Routes: api/dashboard/solar-ordinary-customers/...

    [RoutePrefix("api/dashboard/solar-ordinary-customers")]
    public class SolarOrdinaryCustomersController : ApiController
    {
        private readonly SolarOrdinaryCustomersDao _solarOrdinaryCustomersDao = new SolarOrdinaryCustomersDao();

        /// <summary>GET api/dashboard/solar-ordinary-customers/billcycle/max</summary>
        [HttpGet]
        [Route("billcycle/max")]
        public IHttpActionResult GetMaxBillCycle()
        {
            try
            {
                if (!_solarOrdinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var maxBillCycle = _solarOrdinaryCustomersDao.GetLatestBillCycle();

                return Ok(new
                {
                    data = new { billCycle = maxBillCycle },
                    errorMessage = string.IsNullOrWhiteSpace(maxBillCycle) ? "No bill cycle found in netprogrs." : (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving max bill cycle.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count</summary>
        [HttpGet]
        [Route("count")]
        public IHttpActionResult GetTotalCustomersCount([FromUri] string billCycle = null)
        {
            return GetCustomersCountResponse(billCycle, _solarOrdinaryCustomersDao.GetTotalCustomersCount, "Error retrieving total customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-1</summary>
        [HttpGet]
        [Route("count/net-type-1")]
        public IHttpActionResult GetNetType1CustomersCount([FromUri] string billCycle = null)
        {
            return GetCustomersCountResponse(billCycle, _solarOrdinaryCustomersDao.GetNetMeteringCustomersCount, "Error retrieving net type 1 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-2</summary>
        [HttpGet]
        [Route("count/net-type-2")]
        public IHttpActionResult GetNetType2CustomersCount([FromUri] string billCycle = null)
        {
            return GetCustomersCountResponse(billCycle, _solarOrdinaryCustomersDao.GetNetAccountingCustomersCount, "Error retrieving net type 2 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-3</summary>
        [HttpGet]
        [Route("count/net-type-3")]
        public IHttpActionResult GetNetType3CustomersCount([FromUri] string billCycle = null)
        {
            return GetCustomersCountResponse(billCycle, _solarOrdinaryCustomersDao.GetNetPlusCustomersCount, "Error retrieving net type 3 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-4</summary>
        [HttpGet]
        [Route("count/net-type-4")]
        public IHttpActionResult GetNetType4CustomersCount([FromUri] string billCycle = null)
        {
            return GetCustomersCountResponse(billCycle, _solarOrdinaryCustomersDao.GetNetPlusPlusCustomersCount, "Error retrieving net type 4 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/generation-capacity?billCycle=401&cycles=12</summary>
        [HttpGet]
        [Route("generation-capacity")]
        public IHttpActionResult GetGenerationCapacityGraph([FromUri] string billCycle = null, [FromUri] int cycles = 12)
        {
            try
            {
                if (!_solarOrdinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _solarOrdinaryCustomersDao.GetGenerationCapacityGraph(NormalizeBillCycle(billCycle), cycles);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving solar ordinary generation capacity graph.", errorDetails = ex.Message });
            }
        }

        // ── Shared helper ─────────────────────────────────────────────────────
        private IHttpActionResult GetCustomersCountResponse(
            string billCycle,
            Func<string, SolarOrdinaryCustomersCount> countGetter,
            string fallbackErrorMessage)
        {
            try
            {
                if (!_solarOrdinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = countGetter(NormalizeBillCycle(billCycle));

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = fallbackErrorMessage, errorDetails = ex.Message });
            }
        }

        private static string NormalizeBillCycle(string billCycle)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return null;

            var normalized = billCycle.Trim();

            if ((normalized.StartsWith("{") && normalized.EndsWith("}")) ||
                normalized.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                return null;

            return normalized;
        }
    }


    // =========================================================================
    // 3. SOLAR BULK CUSTOMERS CONTROLLER
    //    Routes: api/dashboard/solar-bulk-customers/...
    // =========================================================================
    [RoutePrefix("api/dashboard/solar-bulk-customers")]
    public class SolarBulkCustomersController : ApiController
    {
        private readonly SolarBulkCustomersDao _solarBulkCustomersDao = new SolarBulkCustomersDao();

        /// <summary>GET api/dashboard/solar-bulk-customers/summary</summary>
        [HttpGet]
        [Route("summary")]
        public IHttpActionResult GetSummary()
        {
            try
            {
                if (!_solarBulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _solarBulkCustomersDao.GetSummary();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving solar bulk customers summary.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count</summary>
        [HttpGet]
        [Route("count")]
        public IHttpActionResult GetTotalCustomersCount()
        {
            return GetCountResponse(_solarBulkCustomersDao.GetTotalCustomersCount, "Error retrieving total solar bulk customers count.");
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-1</summary>
        [HttpGet]
        [Route("count/net-type-1")]
        public IHttpActionResult GetNetType1CustomersCount()
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType1CustomersCount, "Error retrieving net type 1 solar bulk customers count.");
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-2</summary>
        [HttpGet]
        [Route("count/net-type-2")]
        public IHttpActionResult GetNetType2CustomersCount()
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType2CustomersCount, "Error retrieving net type 2 solar bulk customers count.");
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-3</summary>
        [HttpGet]
        [Route("count/net-type-3")]
        public IHttpActionResult GetNetType3CustomersCount()
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType3CustomersCount, "Error retrieving net type 3 solar bulk customers count.");
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-4</summary>
        [HttpGet]
        [Route("count/net-type-4")]
        public IHttpActionResult GetNetType4CustomersCount()
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType4CustomersCount, "Error retrieving net type 4 solar bulk customers count.");
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/generation-capacity?billCycle=401&cycles=12</summary>
        [HttpGet]
        [Route("generation-capacity")]
        public IHttpActionResult GetGenerationCapacityGraph([FromUri] string billCycle = null, [FromUri] int cycles = 12)
        {
            try
            {
                if (!_solarBulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _solarBulkCustomersDao.GetGenerationCapacityGraph(NormalizeBillCycle(billCycle), cycles);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving solar bulk generation capacity graph.", errorDetails = ex.Message });
            }
        }

        // ── Shared helper ─────────────────────────────────────────────────────
        private IHttpActionResult GetCountResponse(
            Func<SolarBulkCustomersCount> countGetter,
            string fallbackErrorMessage)
        {
            try
            {
                if (!_solarBulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = countGetter();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = fallbackErrorMessage, errorDetails = ex.Message });
            }
        }

        private static string NormalizeBillCycle(string billCycle)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return null;

            var normalized = billCycle.Trim();

            if ((normalized.StartsWith("{") && normalized.EndsWith("}")) ||
                normalized.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                return null;

            return normalized;
        }
    }
}