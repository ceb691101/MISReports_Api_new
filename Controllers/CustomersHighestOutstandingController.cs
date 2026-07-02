using System;
using System.Web.Http;
using MISReports_Api.DAL.Collection;
using MISReports_Api.Models.Collection;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/collection")]
    public class CustomersHighestOutstandingController : ApiController
    {
        private readonly CustomersHighestOutstandingDao _dao;

        public CustomersHighestOutstandingController()
        {
            _dao = new CustomersHighestOutstandingDao();
        }

        [HttpPost]
        [Route("highest-outstanding")]
        public IHttpActionResult GetReport([FromBody] CustomersHighestOutstandingRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request payload cannot be null.");

                if (string.IsNullOrEmpty(request.Scope))
                    return BadRequest("Scope (Province/Division) is required.");

                if (request.Scope.Equals("Province", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(request.ProvinceCode))
                    return BadRequest("ProvinceCode is required when Scope is Province.");

                if (request.Scope.Equals("Division", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(request.RegionCode))
                    return BadRequest("RegionCode (Division) is required when Scope is Division.");

                if (request.MonthsInArrears < 0)
                    return BadRequest("MonthsInArrears must be greater than or equal to 0.");

                if (request.OutstandingBalance < 0)
                    return BadRequest("OutstandingBalance must be greater than or equal to 0.");

                // Test connection to the main database first
                if (!_dao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Main database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _dao.GetReportData(request);
                return Ok(new
                {
                    data = data,
                    errorMessage = ""
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR CustomersHighestOutstanding GetReport: {ex.Message}\n{ex.StackTrace}");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Failed to fetch report data.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}
