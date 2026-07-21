using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/chqdetailsexpregion")]
    public class ChqDetailsExpRegionController : ApiController
    {
        private readonly ChqDetailsExpRegionDAL _dal = new ChqDetailsExpRegionDAL();

        // PATH: /api/chqdetailsexpregion/report/2026-01-01/2026-01-31/100?glCode=6001
        // glCode is optional - omit or leave blank to match all account codes.
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{compId}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string compId, [FromUri] string glCode = "")
        {
            return ExecuteQuery(fromDate, toDate, compId, glCode);
        }

        // QUERY: /api/chqdetailsexpregion/report?fromDate=2026-01-01&toDate=2026-01-31&compId=100&glCode=6001
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string compId, [FromUri] string glCode = "")
        {
            return ExecuteQuery(fromDate, toDate, compId, glCode);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string compId, string glCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromDate) || !DateTime.TryParse(fromDate, out DateTime fromDt))
                    return BadRequest("fromDate must be a valid date (e.g., 2026-01-01).");
                if (string.IsNullOrWhiteSpace(toDate) || !DateTime.TryParse(toDate, out DateTime toDt))
                    return BadRequest("toDate must be a valid date (e.g., 2026-01-31).");
                if (toDt < fromDt)
                    return BadRequest("toDate cannot be earlier than fromDate.");
                if (string.IsNullOrWhiteSpace(compId))
                    return BadRequest("compId is required.");

                // SQL uses TO_DATE(:param,'yyyy/mm/dd'), so format to match the mask.
                string fromDateFormatted = fromDt.ToString("yyyy/MM/dd");
                string toDateFormatted = toDt.ToString("yyyy/MM/dd");
                string glCodeValue = glCode?.Trim() ?? "";

                var data = _dal.GetChqDetailsExpRegion(fromDateFormatted, toDateFormatted, compId.Trim(), glCodeValue);

                // Amount column has both positive and negative values (Dr/Cr), so the grand
                // total is a straight sum, not an absolute-value sum.
                var totalDrAmt = data.Sum(x => x.DrAmt ?? 0m);

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy-MM-dd"),
                    toDate = toDt.ToString("yyyy-MM-dd"),
                    compId = compId.Trim(),
                    glCode = glCodeValue,
                    totalRecords = data.Count,
                    totalDrAmt
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}. Use narrower filters.",
                        data = new object[0],
                        summary
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = data.Any() ? "Data retrieved successfully" : "No records found",
                    data,
                    summary
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}