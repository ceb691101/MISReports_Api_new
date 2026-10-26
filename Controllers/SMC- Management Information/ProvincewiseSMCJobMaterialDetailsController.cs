
using System;
using System.Diagnostics;
using System.Globalization;
using System.Web.Http;
using MISReports_Api.DAL.SMC__Management_Information;

namespace MISReports_Api.Controllers.SMC__Management_Information
{
    [RoutePrefix("api/provincewise-smc-job-material-details")]
    public class ProvincewiseSMCJobMaterialDetailsController
        : ApiController
    {
        private readonly ProvincewiseSMCJobMaterialDetailsDAL _dal =
            new ProvincewiseSMCJobMaterialDetailsDAL();

        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(
            string compId,
            string fromDate,
            string toDate,
            string wiringType = "ALL")
        {
            // Validate required parameters.
            if (string.IsNullOrWhiteSpace(compId) ||
                string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest(
                    "compId, fromDate and toDate are required.");
            }

            // Supported date formats.
            string[] formats =
            {
                "yyyy-MM-dd",
                "yyyyMMdd",
                "yyyy/MM/dd"
            };

            // Parse dates.
            if (!DateTime.TryParseExact(
                    fromDate.Trim(),
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fromDt))
            {
                return BadRequest(
                    "Invalid fromDate format. Use yyyy-MM-dd.");
            }

            if (!DateTime.TryParseExact(
                    toDate.Trim(),
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime toDt))
            {
                return BadRequest(
                    "Invalid toDate format. Use yyyy-MM-dd.");
            }

            // Validate date range.
            if (toDt.Date < fromDt.Date)
            {
                return BadRequest(
                    "toDate cannot be earlier than fromDate.");
            }

            // Normalize and validate wiring type.
            string normalizedWiringType =
                string.IsNullOrWhiteSpace(wiringType)
                    ? "ALL"
                    : wiringType.Trim().ToUpperInvariant();

            if (normalizedWiringType != "ALL" &&
                normalizedWiringType != "OH" &&
                normalizedWiringType != "UG")
            {
                return BadRequest(
                    "Invalid wiringType. Allowed values are ALL, OH, and UG.");
            }

            try
            {
                var data =
                    _dal.GetProvincewiseSMCJobMaterialDetails(
                        compId.Trim(),
                        fromDt,
                        toDt,
                        normalizedWiringType);

                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                // Invalid input or validation failure.
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the complete exception for debugging.
                Trace.TraceError(
                    "Error in Provincewise SMC Job Material Details API: {0}",
                    ex.ToString());

                Debug.WriteLine(
                    "========== PROVINCEWISE SMC REPORT ERROR ==========");

                Debug.WriteLine(ex.ToString());

                Debug.WriteLine(
                    "====================================================");

                // Return the full exception temporarily in development
                // so the underlying failure can be diagnosed.
                return InternalServerError(ex);
            }
        }
    }
}
