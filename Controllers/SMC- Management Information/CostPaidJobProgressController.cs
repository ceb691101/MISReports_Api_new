using System;
using System.Diagnostics;
using System.Globalization;
using System.Web.Http;
using System.Net.Http;
using MISReports_Api.DAL.SMC__Management_Information;

namespace MISReports_Api.Controllers.SMC__Management_Information
{
    [RoutePrefix("api/cost-paid-job-progress")]
    public class CostPaidJobProgressController : ApiController
    {
        private readonly CostPaidJobProgressDAL _dal =
            new CostPaidJobProgressDAL();

        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(
            string deptId,
            string applicationNo,
            string fromDate,
            string toDate)
        {
            // Validate required parameters.
            if (string.IsNullOrWhiteSpace(deptId) ||
                string.IsNullOrWhiteSpace(applicationNo) ||
                string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest(
                    "deptId, applicationNo, fromDate and toDate are required.");
            }

            // Supported date formats.
            string[] formats =
            {
                "yyyy-MM-dd",
                "yyyyMMdd",
                "yyyy/MM/dd"
            };

            // Parse fromDate.
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

            // Parse toDate.
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

            try
            {
                var data = _dal.GetCostPaidJobProgress(
                    deptId.Trim(),
                    applicationNo.Trim(),
                    fromDt,
                    toDt);

                var response = Request.CreateResponse(
     System.Net.HttpStatusCode.OK,
     data,
     Configuration.Formatters.JsonFormatter );

                return ResponseMessage(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Error in Cost Paid Job Progress API: {0}",
                    ex.ToString());

                Debug.WriteLine(
                    "========== COST PAID JOB PROGRESS ERROR ==========");

                Debug.WriteLine(ex.ToString());

                Debug.WriteLine(
                    "===================================================");

                return InternalServerError(ex);
            }
        }
    }
}