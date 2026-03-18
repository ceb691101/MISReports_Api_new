using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.Models.SolarInformation;
using MISReports_Api.Models.General;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        private readonly ContractDemandBulkDao _contractDemandBulkDao = new ContractDemandBulkDao();
        private readonly SalesAndCollectionRangeDao _dao = new SalesAndCollectionRangeDao();

        [HttpGet]
        [Route("contract-demand/bulk/area")]
        public IHttpActionResult GetContractDemandAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var validationErrors = new List<string>();

            // Validate required parameters
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

            // Validate required parameters
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

                // Check if data is empty
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
        // GET  api/salesCollection/range
        // ------------------------------------------------------------------ //

       
        [HttpGet]
        [Route("salesCollection/range")]
        public IHttpActionResult GetRange()
        {
            try
            {
                // 1. Test DB connectivity before running any queries
                if (!_dao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                // 2. Fetch data
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
    }
}