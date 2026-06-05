using MISReports_Api.DAL.PhysicalVerification;
using MISReports_Api.Services.Reporting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-obsolete-idle")]
    public class PHVObsoleteIdleController : ApiController
    {
        private readonly PHVObsoleteIdleRepository _repository;
        private readonly PHVObsoleteIdleJasperReportService _jasperReportService;

        public PHVObsoleteIdleController()
        {
            _repository = new PHVObsoleteIdleRepository();
            _jasperReportService = new PHVObsoleteIdleJasperReportService();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetObsoleteIdle(
            string deptId,
            int repYear,
            int repMonth,
            string warehouseCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("Department Id and Warehouse Code are required.");

                var data = await _repository.GetObsoleteIdleAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear,
                    repMonth);

                return Json(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("pdf")]
        public async Task<HttpResponseMessage> GetObsoleteIdlePdf(
            string deptId,
            string deptName,
            int repYear,
            int repMonth,
            string warehouseCode,
            bool download = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "deptId and warehouseCode are required.");
                }

                var data = await _repository.GetObsoleteIdleAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear,
                    repMonth);

                if (data == null || !data.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "No obsolete/idle records were found for the selected period."
                    });
                }

                var costCenterLabel = string.IsNullOrWhiteSpace(deptName)
                    ? deptId.Trim()
                    : $"{deptId.Trim()} - {deptName.Trim()}";

                var pdfBytes = await _jasperReportService.GeneratePhvObsoleteIdlePdfAsync(
                    data,
                    costCenterLabel,
                    warehouseCode.Trim(),
                    repYear,
                    repMonth);

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentLength = pdfBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(download ? "attachment" : "inline")
                {
                    FileName = $"PHV_Obsolete_Idle_{deptId.Trim()}_{warehouseCode.Trim()}_{repYear}_{repMonth:00}.pdf"
                };

                return response;
            }
            catch (JasperReportExecutionException ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error generating obsolete/idle PDF.");
            }
        }
    }
}
