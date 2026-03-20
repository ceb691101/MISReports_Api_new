using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.Models.SolarInformation;
using MISReports_Api.Models.General;
using MISReports_Api.DAL;
using MISReports_Api.Models;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        private readonly ContractDemandBulkDao _contractDemandBulkDao = new ContractDemandBulkDao();
        private readonly SalesAndCollectionRangeDao _dao = new SalesAndCollectionRangeDao();
        private readonly RegisteredCustomersBillCycleDao _smsDao = new RegisteredCustomersBillCycleDao();

        // ------------------------------------------------------------------ //
        // Contract Demand Bulk Reports
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bulk/area")]
        public IHttpActionResult GetContractDemandAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(areaCode))
                validationErrors.Add("Area code is required for area report.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Area,
                AreaCode = areaCode
            };

            return ProcessContractDemandRequest(request);
        }

        [HttpGet]
        [Route("contract-demand/bulk/province")]
        public IHttpActionResult GetContractDemandProvinceReport(
            [FromUri] string billCycle = null,
            [FromUri] string provCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(provCode))
                validationErrors.Add("Province code is required for province report.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Province,
                ProvCode = provCode
            };

            return ProcessContractDemandRequest(request);
        }

        private IHttpActionResult ProcessContractDemandRequest(SecDepositConDemandRequest request)
        {
            try
            {
                if (!_contractDemandBulkDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _contractDemandBulkDao.GetContractDemandBulkReport(request);

                if (data == null || data.Count == 0)
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data available for the specified criteria.",
                        errorDetails = "Please check the bill cycle and location code."
                    });
                }

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in GetContractDemandBulkReport: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get contract demand bulk report data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // Sales Collection Range Report
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("salesCollection/range")]
        public IHttpActionResult GetRange()
        {
            try
            {
                if (!_dao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var result = _dao.GetSalesAndCollectionRange();

                return Ok(new
                {
                    data = result,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve sales and collection range data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // SMS Registered Range Report
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

                if (string.IsNullOrWhiteSpace(fromCycle))
                    validationErrors.Add("From bill cycle is required.");

                if (string.IsNullOrWhiteSpace(toCycle))
                    validationErrors.Add("To bill cycle is required.");

                if (string.IsNullOrWhiteSpace(reportType))
                    validationErrors.Add("Report type is required.");

                if (validationErrors.Count > 0)
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = string.Join("; ", validationErrors)
                    });
                }

                //if (!_smsDao.TestConnection(out string connError))
                //{
                //  return Ok(new
                //{
                //  data = (object)null,
                // errorMessage = "Database connection failed.",
                //errorDetails = connError
                // });
                //}

                var request = new SMSUsageRequest
                {
                    FromBillCycle = fromCycle,
                    ToBillCycle = toCycle,
                    ReportType = reportType,
                    TypeCode = typeCode
                };

                var monthlyData = _smsDao.GetSMSCountRange(request);

                if (monthlyData == null || monthlyData.Count == 0)
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data available for the specified criteria.",
                        errorDetails = "Please check the bill cycle range and location code."
                    });
                }

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
    }
}