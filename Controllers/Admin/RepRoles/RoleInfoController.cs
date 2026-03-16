using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/roleinfo")]
    public class RoleInfoController : ApiController
    {
        private readonly RoleInfoRepository _repository = new RoleInfoRepository();

        [HttpGet]
        [Route("admin")]
        public IHttpActionResult GetAdminRoles()
        {
            try
            {
                var result = _repository.GetAdminRoles();

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = result,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Admin Roles.",
                    errorDetails = ex.Message
                })));
            }
        }

        [HttpGet]
        [Route("user")]
        public IHttpActionResult GetUserRoles()
        {
            try
            {
                var result = _repository.GetUserRoles();

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = result,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get User Roles.",
                    errorDetails = ex.Message
                })));
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateRole([FromBody] CreateRoleRequest request)
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

                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.EpfNo))
                    validationErrors.Add("EpfNo is required.");

                if (string.IsNullOrWhiteSpace(request.RoleId))
                    validationErrors.Add("RoleId is required.");

                if (string.IsNullOrWhiteSpace(request.RoleName))
                    validationErrors.Add("RoleName is required.");

                if (string.IsNullOrWhiteSpace(request.UserType))
                    validationErrors.Add("UserType is required.");

                if (string.IsNullOrWhiteSpace(request.Company))
                    validationErrors.Add("Company is required.");

                if (string.IsNullOrWhiteSpace(request.UserGroup))
                    validationErrors.Add("UserGroup is required.");

                if (string.IsNullOrWhiteSpace(request.CostCentre))
                    validationErrors.Add("CostCentre is required.");

                if (request.LvlNo <= 0)
                    validationErrors.Add("LvlNo must be greater than 0.");

                if (validationErrors.Count > 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = string.Join(" ", validationErrors)
                    }));
                }

                var created = _repository.CreateRole(request);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = created,
                        roleId = request.RoleId,
                        message = "Role created successfully."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot create role.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("companies")]
        public IHttpActionResult GetMotherCompanies()
        {
            try
            {
                var result = _repository.GetMotherCompanies();

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
                    errorMessage = "Cannot get mother companies.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("companies/{companyId}/costcentres")]
        public IHttpActionResult GetCostCentresByCompany(string companyId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Company is required."
                    }));
                }

                var result = _repository.GetCostCentresByCompany(companyId.Trim());

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
                    errorMessage = "Cannot get cost centres.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}