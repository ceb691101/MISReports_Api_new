using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models.Inventory;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/materialmaster")]
    public class MaterialMasterController : ApiController
    {
        private readonly MaterialMasterDAL _dal = new MaterialMasterDAL();
        // PATH: /api/materialmaster/report/RM-1001
        [HttpGet]
        [Route("report/{matCode}")]
        public IHttpActionResult GetReport(string matCode)
        {
            return ExecuteQuery(matCode);
        }
        // QUERY: /api/materialmaster/report?matCode=RM-1001
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string matCode = null)
        {
            return ExecuteQuery(matCode);
        }
        private IHttpActionResult ExecuteQuery(string matCode)
        {
            try
            {
                // matCode is optional: blank/null returns all material master records.
                string matCodeTrimmed = string.IsNullOrWhiteSpace(matCode) ? null : matCode.Trim();
                var data = _dal.GetMaterialMaster(matCodeTrimmed);
                var summary = new
                {
                    matCode = matCodeTrimmed ?? "(all)",
                    totalRecords = data.Count
                };
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