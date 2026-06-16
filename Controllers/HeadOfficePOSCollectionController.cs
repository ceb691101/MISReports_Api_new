using System;
using System.Web.Http;
using MISReports_Api.DAL.Collection;
using MISReports_Api.Models.Collection;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/collection")]
    public class HeadOfficePOSCollectionController : ApiController
    {
        private readonly HeadOfficePOSCollectionDao _dao;

        public HeadOfficePOSCollectionController()
        {
            _dao = new HeadOfficePOSCollectionDao();
        }

        [HttpPost]
        [Route("headofficepos")]
        public IHttpActionResult GetReport([FromBody] HeadOfficePOSCollectionRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request cannot be null.");

                if (string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate))
                    return BadRequest("FromDate and ToDate are required.");

                if (request.ReportType != "Bulk" && request.ReportType != "Ordinary")
                    return BadRequest("Invalid ReportType.");

                var data = _dao.GetReportData(request);
                return Ok(new { data, errorMessage = "" });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Failed to fetch report data.", errorDetails = ex.Message });
            }
        }
    }
}
