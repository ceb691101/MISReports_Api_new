using MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff;
using MISReports_Api.Models.General;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    /// <summary>
    /// Handles general / cross-cutting reports.
    /// Currently exposes the "No. of Consumers by Tariff" endpoints for both
    /// Ordinary and Bulk customer types.
    ///
    /// Route prefix: generalapi
    /// </summary>
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        private readonly ActiveCustomersOrdinaryDao _activeCustomersOrdinaryDao = new ActiveCustomersOrdinaryDao();
        private readonly ActiveCustomersBulkDao _activeCustomersBulkDao = new ActiveCustomersBulkDao();
        private readonly SalesByTariffOrdinaryDao _salesByTariffOrdinaryDao = new SalesByTariffOrdinaryDao();
        private readonly SalesByTariffBulkDao _salesByTariffBulkDao = new SalesByTariffBulkDao();

        // ════════════════════════════════════════════════════════════════════════
        //  ORDINARY – No. of Consumers by Tariff
        //
        //  GET generalapi/active-customers/ordinary
        //      ?fromCycle=2401A&toCycle=2406A
        //      &reportType=area|province|region|entireceb
        //
        //  Returns ALL areas / ALL provinces / ALL regions – no location filter.
        // ════════════════════════════════════════════════════════════════════════
        [HttpGet]
        [Route("activeCustomers/ordinary")]
        public IHttpActionResult GetActiveCustomersOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))
                validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))
                validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":
                    request.ReportType = ActiveCustomersReportType.Area;
                    break;
                case "province":
                    request.ReportType = ActiveCustomersReportType.Province;
                    break;
                case "region":
                    request.ReportType = ActiveCustomersReportType.Region;
                    break;
                case "entireceb":
                    request.ReportType = ActiveCustomersReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessActiveCustomersOrdinaryRequest(request);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  BULK – No. of Consumers by Tariff
        //
        //  GET generalapi/active-customers/bulk
        //      ?fromCycle=2401&toCycle=2406
        //      &reportType=area|province|region|entireceb
        //
        //  Returns ALL areas / ALL provinces / ALL regions – no location filter.
        //  TM1 is always excluded by the DAO layer.
        // ════════════════════════════════════════════════════════════════════════
        [HttpGet]
        [Route("activeCustomers/bulk")]
        public IHttpActionResult GetActiveCustomersBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))
                validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))
                validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":
                    request.ReportType = ActiveCustomersReportType.Area;
                    break;
                case "province":
                    request.ReportType = ActiveCustomersReportType.Province;
                    break;
                case "region":
                    request.ReportType = ActiveCustomersReportType.Region;
                    break;
                case "entireceb":
                    request.ReportType = ActiveCustomersReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessActiveCustomersBulkRequest(request);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Private helpers
        // ════════════════════════════════════════════════════════════════════════
        private IHttpActionResult ProcessActiveCustomersOrdinaryRequest(ActiveCustomersRequest request)
        {
            try
            {
                if (!_activeCustomersOrdinaryDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Ordinary database connection failed.",
                        errorDetails = connError
                    });

                var data = _activeCustomersOrdinaryDao.GetActiveCustomersOrdinaryReport(request);

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve active customers (ordinary) report data.",
                    errorDetails = ex.Message
                });
            }
        }

        private IHttpActionResult ProcessActiveCustomersBulkRequest(ActiveCustomersRequest request)
        {
            try
            {
                if (!_activeCustomersBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Bulk database connection failed.",
                        errorDetails = connError
                    });

                var data = _activeCustomersBulkDao.GetActiveCustomersBulkReport(request);

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve active customers (bulk) report data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ORDINARY – Sales by Tariff
        //
        //  GET generalapi/sales-by-tariff/ordinary
        //      ?fromCycle=439&toCycle=449
        //      &reportType=area|province|region|entireceb
        //
        //  Returns ALL areas / ALL provinces / ALL regions – no location filter.
        //  Aggregated field: KwhSales (sum of cons_kwh).
        // ════════════════════════════════════════════════════════════════════════
        [HttpGet]
        [Route("salesByTariff/ordinary")]
        public IHttpActionResult GetSalesByTariffOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))
                validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))
                validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":
                    request.ReportType = SalesByTariffReportType.Area;
                    break;
                case "province":
                    request.ReportType = SalesByTariffReportType.Province;
                    break;
                case "region":
                    request.ReportType = SalesByTariffReportType.Region;
                    break;
                case "entireceb":
                    request.ReportType = SalesByTariffReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessSalesByTariffOrdinaryRequest(request);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  BULK – Sales by Tariff
        //
        //  GET generalapi/sales-by-tariff/bulk
        //      ?fromCycle=439&toCycle=449
        //      &reportType=area|province|region|entireceb
        //
        //  Returns ALL areas / ALL provinces / ALL regions – no location filter.
        //  TM1 is always excluded. Aggregated field: KwhSales (sum of kwh_units).
        // ════════════════════════════════════════════════════════════════════════
        [HttpGet]
        [Route("salesByTariff/bulk")]
        public IHttpActionResult GetSalesByTariffBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))
                validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))
                validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":
                    request.ReportType = SalesByTariffReportType.Area;
                    break;
                case "province":
                    request.ReportType = SalesByTariffReportType.Province;
                    break;
                case "region":
                    request.ReportType = SalesByTariffReportType.Region;
                    break;
                case "entireceb":
                    request.ReportType = SalesByTariffReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessSalesByTariffBulkRequest(request);
        }

        private IHttpActionResult ProcessSalesByTariffOrdinaryRequest(SalesByTariffRequest request)
        {
            try
            {
                if (!_salesByTariffOrdinaryDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Ordinary database connection failed.",
                        errorDetails = connError
                    });

                var data = _salesByTariffOrdinaryDao.GetSalesByTariffOrdinaryReport(request);

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve sales by tariff (ordinary) report data.",
                    errorDetails = ex.Message
                });
            }
        }

        private IHttpActionResult ProcessSalesByTariffBulkRequest(SalesByTariffRequest request)
        {
            try
            {
                if (!_salesByTariffBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Bulk database connection failed.",
                        errorDetails = connError
                    });

                var data = _salesByTariffBulkDao.GetSalesByTariffBulkReport(request);

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve sales by tariff (bulk) report data.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}