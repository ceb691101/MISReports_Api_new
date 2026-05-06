using MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.General.ListOfGovernmentAccounts;
using MISReports_Api.DAL.General.ListingOfCustomer;
using MISReports_Api.DAL.General.AreasPosition;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.DAL.Shared;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using MISReports_Api.Models.General;
using MISReports_Api.Models.SolarInformation;
using MISReports_Api.Models.Shared;
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
    ///   - Government Accounts (area / department)
    ///   - Areas Position
    ///   - Listing of Customers (area, with optional filters)
    ///
    /// Route prefix: api
    /// </summary>
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        // ── DAO fields ────────────────────────────────────────────────────────
        private readonly ContractDemandBulkDao _contractDemandBulkDao = new ContractDemandBulkDao();
        private readonly ContractDemandBillCycleDao _billCycleDao = new ContractDemandBillCycleDao();
        private readonly SalesAndCollectionRangeDao _dao = new SalesAndCollectionRangeDao();
        private readonly RegisteredCustomersBillCycleDao _smsDao = new RegisteredCustomersBillCycleDao();

        private readonly ActiveCustomersOrdinaryDao _activeCustomersOrdinaryDao = new ActiveCustomersOrdinaryDao();
        private readonly ActiveCustomersBulkDao _activeCustomersBulkDao = new ActiveCustomersBulkDao();

        private readonly SalesByTariffOrdinaryDao _salesByTariffOrdinaryDao = new SalesByTariffOrdinaryDao();
        private readonly SalesByTariffBulkDao _salesByTariffBulkDao = new SalesByTariffBulkDao();

        private readonly ListOfGovernmentAccountsDao _govAccountsDao = new ListOfGovernmentAccountsDao();
        private readonly AreasPositionDao _areasPositionDao = new AreasPositionDao();
        private readonly ListingOfCustomerDao _listingOfCustomerDao = new ListingOfCustomerDao();


        // ================================================================== //
        //  BILL CYCLES                                                         //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/contract-demand/bill-cycles                                 //
        // Response: { data: { MaxBillCycle: "438",                           //
        //                     BillCycles: ["Jan 2026", ...] },               //
        //             errorMessage: null }                                    //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bill-cycles")]
        public IHttpActionResult GetBillCycles()
        {
            try
            {
                var model = _billCycleDao.GetLast24BillCycles();

                if (!string.IsNullOrEmpty(model.ErrorMessage))
                    return Ok(new { data = (object)null, errorMessage = model.ErrorMessage });

                return Ok(new { data = model, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve bill cycles.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  CONTRACT DEMAND BULK                                                //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/contract-demand/bulk/area?billCycle=438&areaCode=43        //
        // Response: { data: [ ...CustomerRecords... ], errorMessage: null }  //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bulk/area")]
        public IHttpActionResult GetContractDemandAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessContractDemandRequest(new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Area,
                AreaCode = areaCode
            });
        }

        // ------------------------------------------------------------------ //
        // GET api/contract-demand/bulk/province?billCycle=438&provCode=D     //
        // Response: { data: [ ...CustomerRecords... ], errorMessage: null }  //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bulk/province")]
        public IHttpActionResult GetContractDemandProvinceReport(
            [FromUri] string billCycle = null,
            [FromUri] string provCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(provCode)) errors.Add("Province code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessContractDemandRequest(new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Province,
                ProvCode = provCode
            });
        }

        private IHttpActionResult ProcessContractDemandRequest(SecDepositConDemandRequest request)
        {
            try
            {
                if (!_contractDemandBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _contractDemandBulkDao.GetContractDemandBulkReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle and location code."
                    });

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessContractDemandRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  ACTIVE CUSTOMERS                                                    //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/activeCustomers/ordinary                                    //
        //     ?fromCycle=2401A&toCycle=2406A                                  //
        //     &reportType=area|province|region|entireceb                      //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("activeCustomers/ordinary")]
        public IHttpActionResult GetActiveCustomersOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = ActiveCustomersReportType.Area; break;
                case "province": request.ReportType = ActiveCustomersReportType.Province; break;
                case "region": request.ReportType = ActiveCustomersReportType.Region; break;
                case "entireceb": request.ReportType = ActiveCustomersReportType.EntireCEB; break;
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

        // ------------------------------------------------------------------ //
        // GET api/activeCustomers/bulk                                        //
        //     ?fromCycle=2401&toCycle=2406                                    //
        //     &reportType=area|province|region|entireceb                      //
        // TM1 is always excluded by the DAO layer.                           //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("activeCustomers/bulk")]
        public IHttpActionResult GetActiveCustomersBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = ActiveCustomersReportType.Area; break;
                case "province": request.ReportType = ActiveCustomersReportType.Province; break;
                case "region": request.ReportType = ActiveCustomersReportType.Region; break;
                case "entireceb": request.ReportType = ActiveCustomersReportType.EntireCEB; break;
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


        // ================================================================== //
        //  SMS REGISTERED RANGE                                                //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/original/smsRegisteredRange                                 //
        //     ?fromCycle=...&toCycle=...&reportType=...&typeCode=...          //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("original/smsRegisteredRange")]
        public IHttpActionResult GetSMSRegisteredRange(
            [FromUri] string fromCycle = null,
            [FromUri] string toCycle = null,
            [FromUri] string reportType = null,
            [FromUri] string typeCode = null)
        {
            try
            {
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("From bill cycle is required.");
                if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("To bill cycle is required.");
                if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("Report type is required.");

                if (validationErrors.Count > 0)
                    return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

                var request = new SMSUsageRequest
                {
                    FromBillCycle = fromCycle,
                    ToBillCycle = toCycle,
                    ReportType = reportType,
                    TypeCode = typeCode
                };

                var monthlyData = _smsDao.GetSMSCountRange(request);

                if (monthlyData == null || monthlyData.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data available for the specified criteria.",
                        errorDetails = "Please check the bill cycle range and location code."
                    });

                return Ok(new
                {
                    data = new Models.SMSRegisteredCustomersModel
                    {
                        LocationName = string.IsNullOrEmpty(typeCode) ? "Entire CEB" : typeCode,
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
                    data = (object)null,
                    errorMessage = "Cannot retrieve SMS registered range data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  SALES BY TARIFF                                                     //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/salesByTariff/ordinary                                      //
        //     ?fromCycle=439&toCycle=449                                       //
        //     &reportType=area|province|region|entireceb                      //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("salesByTariff/ordinary")]
        public IHttpActionResult GetSalesByTariffOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = SalesByTariffReportType.Area; break;
                case "province": request.ReportType = SalesByTariffReportType.Province; break;
                case "region": request.ReportType = SalesByTariffReportType.Region; break;
                case "entireceb": request.ReportType = SalesByTariffReportType.EntireCEB; break;
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

        // ------------------------------------------------------------------ //
        // GET api/salesByTariff/bulk                                          //
        //     ?fromCycle=439&toCycle=449                                       //
        //     &reportType=area|province|region|entireceb                      //
        // TM1 is always excluded. Aggregated field: KwhSales (sum kwh_units). //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("salesByTariff/bulk")]
        public IHttpActionResult GetSalesByTariffBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = SalesByTariffReportType.Area; break;
                case "province": request.ReportType = SalesByTariffReportType.Province; break;
                case "region": request.ReportType = SalesByTariffReportType.Region; break;
                case "entireceb": request.ReportType = SalesByTariffReportType.EntireCEB; break;
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


        // ================================================================== //
        //  GOVERNMENT ACCOUNTS                                                 //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/government-accounts/max-bill-cycle?areaCode=43             //
        // Response: { data: { maxBillCycle: "438" }, errorMessage: null }    //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/max-bill-cycle")]
        public IHttpActionResult GetMaxBillCycle([FromUri] string areaCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(new { data = (object)null, errorMessage = "Area code is required." });

                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var result = _govAccountsDao.GetMaxBillCycle(areaCode);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    return Ok(new { data = (object)null, errorMessage = result.ErrorMessage });

                if (string.IsNullOrEmpty(result.MaxBillCycle))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No bill cycles found for the selected area.",
                        errorDetails = "The area may have no billing data."
                    });

                return Ok(new { data = result, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetMaxBillCycle: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve max bill cycle.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // GET api/government-accounts/departments                            //
        // Response: { data: [ { departmentCode, departmentName }, ... ],    //
        //             errorMessage: null }                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/departments")]
        public IHttpActionResult GetDepartments()
        {
            try
            {
                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _govAccountsDao.GetDepartments();

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No departments found.",
                        errorDetails = "The department table may be empty."
                    });

                return Ok(new { data = data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetDepartments: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve departments.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // GET api/government-accounts/area?billCycle=438&areaCode=43        //
        // Response: { data: [ ...GovernmentAccountRecords... ],             //
        //             errorMessage: null }                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/area")]
        public IHttpActionResult GetGovernmentAccountsAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessGovernmentAccountsRequest(new GovernmentAccountRequest
            {
                BillCycle = billCycle,
                ReportType = "area",
                AreaCode = areaCode
            });
        }

        // ------------------------------------------------------------------ //
        // GET api/government-accounts/department                             //
        //     ?billCycle=438&areaCode=43&departmentCode=ABC                  //
        // Response: { data: [ ...GovernmentAccountRecords... ],             //
        //             errorMessage: null }                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/department")]
        public IHttpActionResult GetGovernmentAccountsDepartmentReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null,
            [FromUri] string departmentCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");
            if (string.IsNullOrWhiteSpace(departmentCode)) errors.Add("Department code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessGovernmentAccountsRequest(new GovernmentAccountRequest
            {
                BillCycle = billCycle,
                ReportType = "department",
                AreaCode = areaCode,
                DepartmentCode = departmentCode
            });
        }

        private IHttpActionResult ProcessGovernmentAccountsRequest(GovernmentAccountRequest request)
        {
            try
            {
                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _govAccountsDao.GetGovernmentAccountsReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle, area code, and department code."
                    });

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessGovernmentAccountsRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get government accounts report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  AREAS POSITION                                                      //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/areas-position/max-bill-cycle?areaCode=43                  //
        // Response: { data: { billCycle: "438" }, errorMessage: null }       //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("areas-position/max-bill-cycle")]
        public IHttpActionResult GetAreasPositionMaxBillCycle([FromUri] string areaCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(new { data = (object)null, errorMessage = "Area code is required." });

                if (!_areasPositionDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var billCycle = _areasPositionDao.GetMaxBillCycle(areaCode);

                if (string.IsNullOrEmpty(billCycle))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No bill cycle found for the selected area.",
                        errorDetails = "The area may have no billing data."
                    });

                return Ok(new
                {
                    data = new { billCycle },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetAreasPositionMaxBillCycle: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve max bill cycle.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // GET api/areas-position/report?areaCode=43[&billCycle=438]          //
        // Omit billCycle to auto-resolve max cycle.                           //
        // Response: {                                                          //
        //   data: {                                                            //
        //     billCycle: "438",                                                //
        //     rows: [ { readerCode, monthlyBill, totalBalance,                //
        //               noOfMonthsInArrears, noOfAccounts }, ... ]            //
        //   },                                                                 //
        //   errorMessage: null                                                 //
        // }                                                                    //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("areas-position/report")]
        public IHttpActionResult GetAreasPositionReport(
            [FromUri] string areaCode = null,
            [FromUri] string billCycle = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(new { data = (object)null, errorMessage = "Area code is required." });

                if (!_areasPositionDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var result = _areasPositionDao.GetAreasPositionReport(new AreasPositionRequest
                {
                    AreaCode = areaCode,
                    BillCycle = billCycle   // null/empty → DAO resolves max automatically
                });

                if (result == null || result.Rows == null || result.Rows.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected area.",
                        errorDetails = "Please check the area code or billing data availability."
                    });

                return Ok(new
                {
                    data = new
                    {
                        billCycle = result.BillCycle,
                        rows = result.Rows
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetAreasPositionReport: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve areas position report.",
                    errorDetails = ex.Message
                });
            }
        }

        // ================================================================== //
        //  LISTING OF CUSTOMERS                                                //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // Max Bill Cycle                                                       //
        // GET api/listing-of-customers/max-bill-cycle?areaCode=43            //
        // Response: { data: { billCycle: "438" }, errorMessage: null }       //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("listing-of-customers/max-bill-cycle")]
        public IHttpActionResult GetListingOfCustomersMaxBillCycle([FromUri] string areaCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(new { data = (object)null, errorMessage = "Area code is required." });

                if (!_listingOfCustomerDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var billCycle = _listingOfCustomerDao.GetMaxBillCycle(areaCode);

                if (string.IsNullOrEmpty(billCycle))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No bill cycle found for the selected area.",
                        errorDetails = "The area may have no billing data."
                    });

                return Ok(new
                {
                    data = new { billCycle },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetListingOfCustomersMaxBillCycle: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve max bill cycle.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // Filter Dropdowns                                                     //
        // GET api/listing-of-customers/filters?areaCode=43&billCycle=438     //
        // Response: {                                                          //
        //   data: {                                                            //
        //     billCycle,                                                       //
        //     tariffs, transformers, phases, connectionTypes,                  //
        //     readerCodes, dailyPacks, depots                                  //
        //   },                                                                 //
        //   errorMessage: null                                                 //
        // }                                                                    //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("listing-of-customers/filters")]
        public IHttpActionResult GetListingOfCustomersFilters(
            [FromUri] string areaCode = null,
            [FromUri] string billCycle = null)
        {
            try
            {
                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");
                if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");

                if (errors.Count > 0)
                    return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

                if (!_listingOfCustomerDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var filters = _listingOfCustomerDao.GetFilters(areaCode, billCycle);

                if (!string.IsNullOrEmpty(filters.ErrorMessage))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Error loading filter options.",
                        errorDetails = filters.ErrorMessage
                    });

                return Ok(new
                {
                    data = filters,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetListingOfCustomersFilters: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot load filter options.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // POST api/listing-of-customers/report                                //
        //                                                                      //
        // Request body (Content-Type: application/json):                       //
        // {                                                                     //
        //   "areaCode":            "43",       ← required                     //
        //   "billCycle":           "438",      ← required                     //
        //   "useTariff":           true,                                        //
        //   "tariff":              "R1",                                        //
        //   "useTransformer":      false,                                       //
        //   "transformer":         null,                                        //
        //   "usePhase":            false,                                       //
        //   "phase":               null,                                        //
        //   "useConnectionType":   false,                                       //
        //   "connectionType":      null,                                        //
        //   "useReaderCode":       false,                                       //
        //   "readerCode":          null,                                        //
        //   "useDailyPack":        false,                                       //
        //   "dailyPackNo":         null,                                        //
        //   "useDepot":            false,                                       //
        //   "depot":               null,                                        //
        //   "useBalance":          true,                                        //
        //   "balanceOperator":     ">=",                                        //
        //   "balanceAmount":       "1000",                                      //
        //   "useLastPaymentDate":  false,                                       //
        //   "lastPaymentOperator": null,                                        //
        //   "lastPaymentDate":     null,                                        //
        //   "useArrearsPosition":  true,                                        //
        //   "arrearsOperator":     ">=",                                        //
        //   "arrearsPosition":     "1"                                          //
        // }                                                                     //
        //                                                                      //
        // Response: { data: [ ...ListingOfCustomerModel... ],                 //
        //             errorMessage: null }                                     //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [Route("listing-of-customers/report")]
        public IHttpActionResult GetListingOfCustomersReport()
        {
            // Read and deserialise the JSON body manually
            ListingOfCustomerRequest request;

            try
            {
                var bodyJson = Request.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrWhiteSpace(bodyJson))
                    return Ok(new { data = (object)null, errorMessage = "Request body is required." });

                request = Newtonsoft.Json.JsonConvert.DeserializeObject<ListingOfCustomerRequest>(bodyJson);

                if (request == null)
                    return Ok(new { data = (object)null, errorMessage = "Request body could not be parsed." });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Invalid JSON in request body.",
                    errorDetails = ex.Message
                });
            }

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.AreaCode)) errors.Add("Area code is required.");
            if (string.IsNullOrWhiteSpace(request.BillCycle)) errors.Add("Bill cycle is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            request.AreaCode = request.AreaCode.Trim();
            request.BillCycle = request.BillCycle.Trim();

            return ProcessListingOfCustomersRequest(request);
        }

        /// <summary>Shared processor — keeps action methods thin.</summary>
        private IHttpActionResult ProcessListingOfCustomersRequest(ListingOfCustomerRequest request)
        {
            try
            {
                if (!_listingOfCustomerDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                // Returns List<ListingOfCustomerModel>
                var data = _listingOfCustomerDao.GetListingOfCustomerReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle, area code, and filter values."
                    });

                // Return flat array — frontend maps: json.data as ListingOfCustomerModel[]
                return Ok(new
                {
                    data = data,   // List<ListingOfCustomerModel>
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessListingOfCustomersRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get listing of customers report data.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}