using MISReports_Api.DAL.SolarJobs;
using MISReports_Api.Models.SolarJobs;
using MISReports_Api.Services.Reporting;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/solarjobs/pending-jobs")]
    public class SolarPendingJobsController : ApiController
    {
        private readonly SolarPendingJobsRepository _repository = new SolarPendingJobsRepository();
        private readonly SolarPendingJobsJasperReportService _jasperReportService = new SolarPendingJobsJasperReportService();

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetList([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string provinceId)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate) || string.IsNullOrWhiteSpace(provinceId))
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "fromDate, toDate and provinceId are required."
                });
            }

            if (!TryParseDate(fromDate, out DateTime parsedFromDate) || !TryParseDate(toDate, out DateTime parsedToDate))
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Invalid date format. Use yyyy/MM/dd, yyyy-MM-dd, or yyyyMMdd."
                });
            }

            if (parsedFromDate.Date > parsedToDate.Date)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "fromDate cannot be greater than toDate."
                });
            }

            try
            {
                var data = await _repository.GetPendingJobsAsync(parsedFromDate, parsedToDate, provinceId);

                return Ok(new
                {
                    data,
                    errorMessage = (string)null,
                    count = data.Count
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Solar Pending Jobs report data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("pdf")]
        public async Task<System.Net.Http.HttpResponseMessage> GetPdf([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string provinceId, [FromUri] bool download = false)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate) || string.IsNullOrWhiteSpace(provinceId))
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, "fromDate, toDate and provinceId are required.");
            }

            if (!TryParseDate(fromDate, out DateTime parsedFromDate) || !TryParseDate(toDate, out DateTime parsedToDate))
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, "Invalid date format.");
            }

            try
            {
                var data = await _repository.GetPendingJobsAsync(parsedFromDate, parsedToDate, provinceId);
                if (data == null || data.Count == 0)
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.NotFound, "No data found for the selected criteria.");
                }

                byte[] pdfBytes = await _jasperReportService.GeneratePendingJobsPdfAsync(
                    data,
                    provinceId,
                    parsedFromDate.ToString("yyyy-MM-dd"),
                    parsedToDate.ToString("yyyy-MM-dd")
                );

                var response = Request.CreateResponse(System.Net.HttpStatusCode.OK);
                response.Content = new System.Net.Http.ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

                var fileName = $"Solar_Pending_Jobs_{provinceId}_{fromDate}.pdf";
                var dispositionType = download ? "attachment" : "inline";
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue(dispositionType)
                {
                    FileName = fileName
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private static bool TryParseDate(string value, out DateTime result)
        {
            return DateTime.TryParseExact(value, new[] { "yyyy/MM/dd", "yyyy-MM-dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
    }
}
