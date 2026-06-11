using MISReports_Api.DAL.CollectionInformation;
using MISReports_Api.Models.CollectionInformation;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("apicollection")]
    public class CollectionController : ApiController
    {
        private readonly ReceivePositionDao _receivePositionDao = new ReceivePositionDao();

        // -----------------------------------------------------------------------
        // GET apicollection/receive-position-dropdowns
        //
        // Returns bill cycles, bill types, areas, and provinces (from prov_servers).
        // Called once when the form loads to populate all dropdown lists.
        // -----------------------------------------------------------------------

        [HttpGet]
        [Route("receive-position-dropdowns")]
        public IHttpActionResult GetReceivePositionDropdowns()
        {
            try
            {
                if (!_receivePositionDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var dropdowns = _receivePositionDao.GetDropdowns();

                return Ok(new
                {
                    data = dropdowns,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot load dropdown data.",
                    errorDetails = ex.Message
                });
            }
        }

        // -----------------------------------------------------------------------
        // GET apicollection/receive-position
        //     ?billCycle=438&billType=O&areaCode=01
        //
        // areaCode may be:
        //   - an individual area code  (e.g. "01")
        //   - a province code          (e.g. "WP")  → DAO expands to all areas in province
        //   - "CEB"                                  → DAO returns all areas
        //
        // billType : "O" (Ordinary) or "B" (Bulk)
        // -----------------------------------------------------------------------

        [HttpGet]
        [Route("receive-position")]
        public IHttpActionResult GetReceivePositionReport(
            [FromUri] string billCycle = null,
            [FromUri] string billType = null,
            [FromUri] string areaCode = null)
        {
            // Strip any accidental surrounding quotes from URL params
            billCycle = StripQuotes(billCycle);
            billType = StripQuotes(billType);
            areaCode = StripQuotes(areaCode);

            // Validate
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(billType))
                validationErrors.Add("Bill type is required.");
            else if (billType.ToUpper() != "O" && billType.ToUpper() != "B")
                validationErrors.Add("Bill type must be O (Ordinary) or B (Bulk).");

            if (string.IsNullOrWhiteSpace(areaCode))
                validationErrors.Add("Area code is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new ReceivePositionRequest
            {
                BillCycle = billCycle.Trim(),
                BillType = billType.Trim().ToUpper(),
                AreaCode = areaCode.Trim()
            };

            return ProcessReceivePositionRequest(request);
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        private IHttpActionResult ProcessReceivePositionRequest(ReceivePositionRequest request)
        {
            try
            {
                if (!_receivePositionDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _receivePositionDao.GetReceivePositionReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get receive position report data.",
                    errorDetails = ex.Message
                });
            }
        }

        // Removes surrounding ' or " that the frontend may include in the URL
        private static string StripQuotes(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            value = value.Trim();
            if ((value.StartsWith("'") && value.EndsWith("'")) ||
                (value.StartsWith("\"") && value.EndsWith("\"")))
                value = value.Substring(1, value.Length - 2);
            return value;
        }
    }
}