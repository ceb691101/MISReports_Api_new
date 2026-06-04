using MISReports_Api.DAL.SolarJobs;
using MISReports_Api.Models.SolarJobs;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Http;
using MISReports_Api.Services.Reporting;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/solarjobs/ccapplication")]
    public class CcApplicationController : ApiController
    {
        private readonly CcApplicationRepository _repository = new CcApplicationRepository();
        private readonly CcApplicationJasperReportService _jasperReportService = new CcApplicationJasperReportService();

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetList([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string costctr)
        {
            // ... (existing code for list)
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate) || string.IsNullOrWhiteSpace(costctr))
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "fromDate, toDate and costctr are required."
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
                var data = await _repository.GetApplicationsAsync(parsedFromDate, parsedToDate, costctr);

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
                    errorMessage = "Cannot get Cc Application report data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("pdf")]
        public async Task<HttpResponseMessage> GetPdf([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string costctr, [FromUri] string costctrName = null, [FromUri] bool download = false)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate) || string.IsNullOrWhiteSpace(costctr))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "fromDate, toDate and costctr are required.");
            }

            if (!TryParseDate(fromDate, out DateTime parsedFromDate) || !TryParseDate(toDate, out DateTime parsedToDate))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid date format.");
            }

            try
            {
                var data = await _repository.GetApplicationsAsync(parsedFromDate, parsedToDate, costctr);
                if (data == null || data.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No data found for the selected criteria.");
                }

                string costCenterLabel = string.IsNullOrWhiteSpace(costctrName) ? costctr : $"{costctr} - {costctrName}";
                
                byte[] pdfBytes = await _jasperReportService.GenerateCcApplicationPdfAsync(
                    data,
                    costCenterLabel,
                    parsedFromDate.ToString("yyyy-MM-dd"),
                    parsedToDate.ToString("yyyy-MM-dd")
                );

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                var fileName = $"Cc_Application_Progress_{costctr}_{fromDate}.pdf";
                var dispositionType = download ? "attachment" : "inline";
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(dispositionType)
                {
                    FileName = fileName
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private static bool TryParseDate(string value, out DateTime result)
        {
            return DateTime.TryParseExact(value, new[] { "yyyy/MM/dd", "yyyy-MM-dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
    }
}