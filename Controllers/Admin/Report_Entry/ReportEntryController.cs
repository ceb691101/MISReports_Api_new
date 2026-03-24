using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/reportentry")]
    public class ReportEntryController : ApiController
    {
        private readonly ReportEntryRepository _repository = new ReportEntryRepository();

        [HttpGet]
        [Route("nextid")]
        public IHttpActionResult GetNextReportIdNo()
        {
            try
            {
                var id = _repository.GetNextReportIdNo();
                return Ok(JObject.FromObject(new { data = id, errorMessage = (string)null }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "Cannot get next ID.", errorDetails = ex.Message }));
            }
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllReportEntries()
        {
            try
            {
                var result = _repository.GetAllReportEntries();
                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report entries.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("filter")]
        public IHttpActionResult FilterReportEntries([FromUri] string repid, [FromUri] string catcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repid) || string.IsNullOrWhiteSpace(catcode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "repid and catcode are required."
                    }));
                }

                var result = _repository.FilterReportEntries(repid, catcode);
                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot filter report entries.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult AddReportEntry([FromBody] ReportEntryModel request)
        {
            try
            {
                if (request == null)
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "Request body is required." }));

                if (string.IsNullOrWhiteSpace(request.RepId) || string.IsNullOrWhiteSpace(request.CatCode))
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "RepId and CatCode are required." }));

                var success = _repository.AddReportEntry(request);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = success, message = success ? "Report entry added successfully." : "Failed to add report entry." },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = new { success = false },
                    errorMessage = "Cannot add report entry.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPut]
        [Route("{repid}")]
        public IHttpActionResult EditReportEntry(string repid, [FromBody] ReportEntryModel request)
        {
            try
            {
                if (request == null)
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "Request body is required." }));

                request.RepId = repid;

                var success = _repository.EditReportEntry(request);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = success, message = success ? "Report entry updated successfully." : "Report entry not found." },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = new { success = false },
                    errorMessage = "Cannot edit report entry.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpDelete]
        [Route("{repid}")]
        public IHttpActionResult DeleteReportEntry(string repid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repid))
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "RepId is required." }));

                var success = _repository.DeleteReportEntry(repid);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = success, message = success ? "Report entry deleted successfully." : "Report entry not found or is restricted by constraints." },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = new { success = false },
                    errorMessage = "Cannot delete report entry.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}
