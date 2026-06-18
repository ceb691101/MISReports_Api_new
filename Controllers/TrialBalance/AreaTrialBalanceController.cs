using MISReports_Api.DAL;
using MISReports_Api.Models;
using MISReports_Api.Services.Reporting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.TrialBalance
{
    [RoutePrefix("api/areatrialbalance")]
    public class AreaTrialBalanceController : ApiController
    {
        private readonly AreaTrialBalanceRepository _repository;
        private readonly AreaTrialBalanceJasperReportService _jasperReportService;

        public AreaTrialBalanceController()
        {
            _repository = new AreaTrialBalanceRepository();
            _jasperReportService = new AreaTrialBalanceJasperReportService();
        }

        [HttpGet]
        [Route("list")]
        public IHttpActionResult GetAreaTrialBalanceList(
            [FromUri] string companyId,
            [FromUri] int year,
            [FromUri] int month)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                    return BadRequest("companyId is required.");
                if (year <= 0)
                    return BadRequest("year is required.");
                if (month <= 0 || month > 12)
                    return BadRequest("month must be between 1 and 12.");

                var data = _repository.GetAreaTrialBalanceData(companyId.Trim(), year, month);

                return Json(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("pdf")]
        public async Task<HttpResponseMessage> GetAreaTrialBalancePdf(
            [FromUri] string companyId,
            [FromUri] int year,
            [FromUri] int month,
            [FromUri] bool download = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "companyId is required.");
                if (year <= 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "year is required.");
                if (month <= 0 || month > 12)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "month must be between 1 and 12.");

                var data = _repository.GetAreaTrialBalanceData(companyId.Trim(), year, month);

                if (data == null || !data.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "No trial balance records were found for the selected period."
                    });
                }

                var pdfBytes = await _jasperReportService.GenerateAreaTrialBalancePdfAsync(
                    data.Cast<object>(),
                    companyId.Trim(),
                    year,
                    month);

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentLength = pdfBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(download ? "attachment" : "inline")
                {
                    FileName = $"Area_Trial_Balance_{companyId.Trim()}_{year}_{month:00}.pdf"
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"Error generating trial balance PDF: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("csv")]
        public async Task<HttpResponseMessage> GetAreaTrialBalanceCsv(
            [FromUri] string companyId,
            [FromUri] int year,
            [FromUri] int month)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "companyId is required.");
                if (year <= 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "year is required.");
                if (month <= 0 || month > 12)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "month must be between 1 and 12.");

                var data = _repository.GetAreaTrialBalanceData(companyId.Trim(), year, month);

                if (data == null || !data.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "No trial balance records were found for the selected period."
                    });
                }

                var csvBytes = await _jasperReportService.GenerateAreaTrialBalanceCsvAsync(
                    data.Cast<object>(),
                    companyId.Trim(),
                    year,
                    month);

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(csvBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                response.Content.Headers.ContentLength = csvBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"Area_Trial_Balance_{companyId.Trim()}_{year}_{month:00}.csv"
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"Error generating trial balance CSV: {ex.Message}");
            }
        }
    }
}
