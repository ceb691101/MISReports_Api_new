using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL.Admin.Report_Parameters;
using MISReports_Api.Models.Admin.Report_Parameters;
using Newtonsoft.Json.Linq;

namespace MISReports_Api.Controllers.Admin.Report_Parameters
{
    [RoutePrefix("api/reppara")]
    public class RepParaController : ApiController
    {
        private readonly RepParaRepository _repository = new RepParaRepository();

        [HttpPost]
        [Route("save-reportparams")]
        [Route("SAVE_REPORTPARAMS")]
        public IHttpActionResult SaveReportParams([FromBody] RepParaModel request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Request body is required."
                    }));
                }

                if (string.IsNullOrWhiteSpace(request.ParaId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "ParaId is required."
                    }));
                }

                var result = _repository.SaveReportParams(request.ParaId, request.ParaDesc);

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
                    errorMessage = "Cannot save report parameters.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("populateparamts")]
        [Route("POPULATEPARAMTS")]
        public IHttpActionResult PopulateParamTs([FromBody] PopulateParamTsModel request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Request body is required."
                    }));
                }

                if (string.IsNullOrWhiteSpace(request.RepId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "RepId is required."
                    }));
                }

                var result = _repository.PopulateParamTs(request.RepId, request.ParamList);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        updatedRows = result.UpdatedRows,
                        success = result.UpdatedRows > 0
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot populate parameter list.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}
