using MISReports_Api.DAL.Collection.ReceivablePosition;
using MISReports_Api.Models.Collection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    /// <summary>
    /// Handles collection-related reports.
    /// Exposes endpoints for:
    ///   - Receivable Position (area, by bill cycle and bill type)
    ///
    /// Route prefix: api
    /// </summary>
    [RoutePrefix("api")]
    public class CollectionController : ApiController
    {
        // ── DAO fields ────────────────────────────────────────────────────────
        private readonly ReceivablePositionDao _receivablePositionDao = new ReceivablePositionDao();


        // ================================================================== //
        //  RECEIVABLE POSITION                                                 //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/receivable-position/report                                  //
        //     ?billCycle=454&areaCode=43&billType=O                           //
        // Response: { data: [ ...ReceivablePositionModel... ],                //
        //             errorMessage: null }                                     //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("receivable-position/report")]
        public IHttpActionResult GetReceivablePositionReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null,
            [FromUri] string billType = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");
            if (string.IsNullOrWhiteSpace(billType)) errors.Add("Bill type is required.");

            if (errors.Count > 0)
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", errors)
                }));

            try
            {
                if (!_receivablePositionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));

                var request = new ReceivablePositionRequest
                {
                    BillCycle = billCycle.Trim(),
                    AreaCode = areaCode.Trim(),
                    BillType = billType.Trim().ToUpper()
                };

                var data = _receivablePositionDao.GetReceivablePositionReport(request);

                if (data == null || data.Count == 0)
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle, area code, and bill type."
                    }));

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetReceivablePositionReport: {ex.Message}\n{ex.StackTrace}");

                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get receivable position report data.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ------------------------------------------------------------------ //
        // GET api/receivable-position/areas-by-province?provinceCode=3       //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("receivable-position/areas-by-province")]
        public IHttpActionResult GetReceivablePositionAreasByProvince(
            [FromUri] string provinceCode = null,
            [FromUri] string billType = null,
            [FromUri] string billCycle = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Province code is required."
                }));

            try
            {
                if (!_receivablePositionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));

                var data = _receivablePositionDao.GetAreasByProvince(
                    provinceCode.Trim(),
                    billType,
                    billCycle);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas for province.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ------------------------------------------------------------------ //
        // GET api/receivable-position/areas-by-region?regionCode=R1          //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("receivable-position/areas-by-region")]
        public IHttpActionResult GetReceivablePositionAreasByRegion(
            [FromUri] string regionCode = null,
            [FromUri] string billType = null,
            [FromUri] string billCycle = null)
        {
            if (string.IsNullOrWhiteSpace(regionCode))
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Region code is required."
                }));

            try
            {
                if (!_receivablePositionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));

                var data = _receivablePositionDao.GetAreasByRegion(
                    regionCode.Trim(),
                    billType,
                    billCycle);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas for region.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ------------------------------------------------------------------ //
        // GET api/receivable-position/bill-types                             //
        // Response: { data: [ { billType: "O", displayName: "Ordinary" },   //
        //                      { billType: "B", displayName: "Bulk" } ],     //
        //             errorMessage: null }                                    //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("receivable-position/bill-types")]
        public IHttpActionResult GetReceivablePositionBillTypes()
        {
            try
            {
                if (!_receivablePositionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));

                var rawTypes = _receivablePositionDao.GetDistinctBillTypes();

                if (rawTypes == null || rawTypes.Count == 0)
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "No bill types found in receive_position table."
                    }));

                // Map raw bill_type codes to display names
                var billTypes = rawTypes.Select(bt => new ReceivablePositionBillTypeModel
                {
                    BillType = bt,
                    DisplayName = bt == "O" ? "Ordinary"
                                : bt == "B" ? "Bulk"
                                : bt
                }).ToList();

                return Ok(JObject.FromObject(new
                {
                    data = billTypes,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetReceivablePositionBillTypes: {ex.Message}\n{ex.StackTrace}");

                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get bill types.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}