using MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using MISReports_Api.Models.General;
using MISReports_Api.Models.SolarInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    /// <summary>
    /// Handles general / cross-cutting reports.
    /// Exposes endpoints for:
    ///   - Contract Demand Bulk (area / province)
    ///   - Active Customers Ordinary and Bulk (area / province / region / entireceb)
    ///   - Sales by Tariff Ordinary and Bulk (area / province / region / entireceb)
    ///   - Sales and Collection Range
    ///   - SMS Registered Range
    ///
    /// Route prefix: api
    /// </summary>
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        // ── DAO fields ────────────────────────────────────────────────────────
        private readonly ContractDemandBulkDao            _contractDemandBulkDao    = new ContractDemandBulkDao();
        private readonly SalesAndCollectionRangeDao       _dao                      = new SalesAndCollectionRangeDao();
        private readonly RegisteredCustomersBillCycleDao  _smsDao                   = new RegisteredCustomersBillCycleDao();

        private readonly ActiveCustomersOrdinaryDao       _activeCustomersOrdinaryDao = new ActiveCustomersOrdinaryDao();
        private readonly ActiveCustomersBulkDao           _activeCustomersBulkDao     = new ActiveCustomersBulkDao();

        private readonly SalesByTariffOrdinaryDao         _salesByTariffOrdinaryDao   = new SalesByTariffOrdinaryDao();
        private readonly SalesByTariffBulkDao             _salesByTariffBulkDao       = new SalesByTariffBulkDao();

        // ════════════════════════════════════════════════════════════════════
        //  CONTRACT DEMAND BULK REPORTS
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("contract-demand/bulk/area")]
        public IHttpActionResult GetContractDemandAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode  = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(areaCode))
                validationErrors.Add("Area code is required for area report.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SecDepositConDemandRequest
            {
                BillCycle  = billCycle,
                ReportType = SolarReportType.Area,
                AreaCode   = areaCode
            };

            return ProcessContractDemandRequest(request);
        }

        [HttpGet]
        [Route("contract-demand/bulk/province")]
        public IHttpActionResult GetContractDemandProvinceReport(
            [FromUri] string billCycle = null,
            [FromUri] string provCode  = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(provCode))
                validationErrors.Add("Province code is required for province report.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SecDepositConDemandRequest
            {
                BillCycle  = billCycle,
                ReportType = SolarReportType.Province,
                ProvCode   = provCode
            };

            return ProcessContractDemandRequest(request);
        }

        private IHttpActionResult ProcessContractDemandRequest(SecDepositConDemandRequest request)
        {
            try
            {
                if (!_contractDemandBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data         = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _contractDemandBulkDao.GetContractDemandBulkReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data         = (object)null,
                        errorMessage = "No data available for the specified criteria.",
                        errorDetails = "Please check the bill cycle and location code."
                    });

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in GetContractDemandBulkReport: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Ok(new
                {
                    data         = (object)null,
                    errorMessage = "Cannot get contract demand bulk report data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  ORDINARY – No. of Consumers by Tariff (Active Customers)
        //
        //  GET api/activeCustomers/ordinary
        //      ?fromCycle=2401A&toCycle=2406A&reportType=area|province|region|entireceb
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("activeCustomers/ordinary")]
        public IHttpActionResult GetActiveCustomersOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))   validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))     validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))  validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle   = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":       request.ReportType = ActiveCustomersReportType.Area;       break;
                case "province":   request.ReportType = ActiveCustomersReportType.Province;   break;
                case "region":     request.ReportType = ActiveCustomersReportType.Region;     break;
                case "entireceb":  request.ReportType = ActiveCustomersReportType.EntireCEB;  break;
                default:
                    return Ok(new
                    {
                        data         = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessActiveCustomersOrdinaryRequest(request);
        }

        // ════════════════════════════════════════════════════════════════════
        //  BULK – No. of Consumers by Tariff (Active Customers)
        //
        //  GET api/activeCustomers/bulk
        //      ?fromCycle=2401&toCycle=2406&reportType=area|province|region|entireceb
        //  TM1 is always excluded by the DAO layer.
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("activeCustomers/bulk")]
        public IHttpActionResult GetActiveCustomersBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))   validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))     validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))  validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle   = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":       request.ReportType = ActiveCustomersReportType.Area;       break;
                case "province":   request.ReportType = ActiveCustomersReportType.Province;   break;
                case "region":     request.ReportType = ActiveCustomersReportType.Region;     break;
                case "entireceb":  request.ReportType = ActiveCustomersReportType.EntireCEB;  break;
                default:
                    return Ok(new
                    {
                        data         = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessActiveCustomersBulkRequest(request);
        }

        private IHttpActionResult ProcessActiveCustomersOrdinaryRequest(ActiveCustomersRequest request)
        {
            try
            {
                if (!_activeCustomersOrdinaryDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data         = (object)null,
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
                    data         = (object)null,
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
                        data         = (object)null,
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
                    data         = (object)null,
                    errorMessage = "Cannot retrieve active customers (bulk) report data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SALES COLLECTION RANGE
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("salesCollection/range")]
        public IHttpActionResult GetRange()
        {
            try
            {
                if (!_dao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data         = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var result = _dao.GetSalesAndCollectionRange();
                return Ok(new { data = result, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data         = (object)null,
                    errorMessage = "Cannot retrieve sales and collection range data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SMS REGISTERED RANGE
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("original/smsRegisteredRange")]
        public IHttpActionResult GetSMSRegisteredRange(
            [FromUri] string fromCycle  = null,
            [FromUri] string toCycle    = null,
            [FromUri] string reportType = null,
            [FromUri] string typeCode   = null)
        {
            try
            {
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(fromCycle))   validationErrors.Add("From bill cycle is required.");
                if (string.IsNullOrWhiteSpace(toCycle))     validationErrors.Add("To bill cycle is required.");
                if (string.IsNullOrWhiteSpace(reportType))  validationErrors.Add("Report type is required.");

                if (validationErrors.Count > 0)
                    return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

                var request = new SMSUsageRequest
                {
                    FromBillCycle = fromCycle,
                    ToBillCycle   = toCycle,
                    ReportType    = reportType,
                    TypeCode      = typeCode
                };

                var monthlyData = _smsDao.GetSMSCountRange(request);

                if (monthlyData == null || monthlyData.Count == 0)
                    return Ok(new
                    {
                        data         = (object)null,
                        errorMessage = "No data available for the specified criteria.",
                        errorDetails = "Please check the bill cycle range and location code."
                    });

                return Ok(new
                {
                    data = new Models.SMSRegisteredCustomersModel
                    {
                        LocationName  = string.IsNullOrEmpty(typeCode) ? "Entire CEB" : typeCode,
                        MonthlyCounts = monthlyData
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in GetSMSRegisteredRange: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Ok(new
                {
                    data         = (object)null,
                    errorMessage = "Cannot retrieve SMS registered range data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  ORDINARY – Sales by Tariff
        //
        //  GET api/salesByTariff/ordinary
        //      ?fromCycle=439&toCycle=449&reportType=area|province|region|entireceb
        //  Aggregated field: KwhSales (sum of cons_kwh).
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("salesByTariff/ordinary")]
        public IHttpActionResult GetSalesByTariffOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))   validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))     validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))  validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle   = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":       request.ReportType = SalesByTariffReportType.Area;       break;
                case "province":   request.ReportType = SalesByTariffReportType.Province;   break;
                case "region":     request.ReportType = SalesByTariffReportType.Region;     break;
                case "entireceb":  request.ReportType = SalesByTariffReportType.EntireCEB;  break;
                default:
                    return Ok(new
                    {
                        data         = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessSalesByTariffOrdinaryRequest(request);
        }

        // ════════════════════════════════════════════════════════════════════
        //  BULK – Sales by Tariff
        //
        //  GET api/salesByTariff/bulk
        //      ?fromCycle=439&toCycle=449&reportType=area|province|region|entireceb
        //  TM1 is always excluded. Aggregated field: KwhSales (sum of kwh_units).
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("salesByTariff/bulk")]
        public IHttpActionResult GetSalesByTariffBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromCycle))   validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle))     validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType))  validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle   = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area":       request.ReportType = SalesByTariffReportType.Area;       break;
                case "province":   request.ReportType = SalesByTariffReportType.Province;   break;
                case "region":     request.ReportType = SalesByTariffReportType.Region;     break;
                case "entireceb":  request.ReportType = SalesByTariffReportType.EntireCEB;  break;
                default:
                    return Ok(new
                    {
                        data         = (object)null,
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
                        data         = (object)null,
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
                    data         = (object)null,
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
                        data         = (object)null,
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
                    data         = (object)null,
                    errorMessage = "Cannot retrieve sales by tariff (bulk) report data.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}