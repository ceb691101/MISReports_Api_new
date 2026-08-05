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
    [RoutePrefix("api/quantitymatfifo")]
    public class QuantityMatFIFOController : ApiController
    {
        private readonly QuantityMatFIFODAL _dal = new QuantityMatFIFODAL();

        // PATH: /api/quantitymatfifo/report/matcode/whcode
        [HttpGet]
        [Route("report/{matCode}/{whCode}")]
        public IHttpActionResult GetReport(string matCode, string whCode)
        {
            return ExecuteQuery(matCode, whCode);
        }

        // QUERY: /api/quantitymatfifo/report?matCode=...&whCode=...
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string matCode, [FromUri] string whCode)
        {
            return ExecuteQuery(matCode, whCode);
        }

        private IHttpActionResult ExecuteQuery(string matCode, string whCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(whCode))
                    return BadRequest("whCode is required.");

                // matCode is a LIKE prefix filter; an empty value matches all material codes.
                string matCodeTrimmed = (matCode ?? string.Empty).Trim();
                string whCodeTrimmed = whCode.Trim();

                var data = _dal.GetQuantityMatFIFO(matCodeTrimmed, whCodeTrimmed);
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    matCode = matCodeTrimmed,
                    whCode = whCodeTrimmed,
                    totalRecords = data.Count,
                    totalQtyOnHand = data.Sum(x => x.QtyOnHand ?? 0m),
                    totalValue = data.Sum(x => x.Value ?? 0m)
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