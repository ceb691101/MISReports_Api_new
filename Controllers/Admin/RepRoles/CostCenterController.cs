using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/costcenters")]
    public class CostCenterController : ApiController
    {
        private readonly CostCenterRepository _repository = new CostCenterRepository();

        /// <summary>
        /// Load cost centers for a company and get assigned cost centers for a role
        /// GET /api/costcenters/load?companyId=DISCO4&roleId=admin1
        /// </summary>
        [HttpGet]
        [Route("load")]
        public IHttpActionResult LoadCostCenters(string companyId, string roleId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                    {
                        data = (object)null,
                        errorMessage = "Company ID is required."
                    })));
                }

                // Get company details
                var company = _repository.GetCompanyDetails(companyId);

                // Get all available cost centers for the company
                var availableCostCenters = _repository.GetCostCentersForCompany(companyId);

                // Get cost centers assigned to the role (if provided)
                var selectedCostCenters = new List<string>();
                if (!string.IsNullOrWhiteSpace(roleId))
                {
                    selectedCostCenters = _repository.GetAssignedCostCenters(roleId);
                }

                // Mark selected cost centers
                foreach (var cc in availableCostCenters)
                {
                    cc.IsSelected = selectedCostCenters.Contains(cc.CostCenterId);
                }

                var response = new CostCenterLoadResponse
                {
                    Company = company,
                    AvailableCostCenters = availableCostCenters,
                    SelectedCostCenters = selectedCostCenters,
                    ErrorMessage = null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = response,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Failed to load cost centers.",
                    errorDetails = ex.Message
                })));
            }
        }

        /// <summary>
        /// Save role-to-cost-center associations
        /// POST /api/costcenters/save
        /// Body: { "roleId": "admin1", "companyId": "DISCO4", "costCenterIds": ["974.00", "971.00"] }
        /// </summary>
        [HttpPost]
        [Route("save")]
        public IHttpActionResult SaveCostCenters([FromBody] CostCenterSaveRequest request)
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

                if (string.IsNullOrWhiteSpace(request.RoleId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "RoleId is required."
                    }));
                }

                if (string.IsNullOrWhiteSpace(request.CompanyId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "CompanyId is required."
                    }));
                }

                if (request.CostCenterIds == null)
                {
                    request.CostCenterIds = new List<string>();
                }

                // Save the role-cost center associations with company
                _repository.SaveRoleCostCenters(request.RoleId, request.CompanyId, request.CostCenterIds);

                return Ok(JObject.FromObject(new
                {
                    data = new { message = $"Successfully saved {request.CostCenterIds.Count} cost center assignments for role {request.RoleId} in company {request.CompanyId}." },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Failed to save cost center assignments.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Get assigned cost centers for a specific role
        /// GET /api/costcenters/assigned?roleId=admin1
        /// </summary>
        [HttpGet]
        [Route("assigned")]
        public IHttpActionResult GetAssignedCostCenters(string roleId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleId))
                {
                    return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                    {
                        data = (object)null,
                        errorMessage = "RoleId is required."
                    })));
                }

                var assignedCostCenters = _repository.GetAssignedCostCenters(roleId);

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = assignedCostCenters,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Failed to load assigned cost centers.",
                    errorDetails = ex.Message
                })));
            }
        }

        /// <summary>
        /// Get all cost centers for a company
        /// GET /api/costcenters/company?companyId=DISCO4
        /// Returns department data formatted as "ID:Name"
        /// </summary>
        [HttpGet]
        [Route("company")]
        public IHttpActionResult GetCompanyCostCenters(string companyId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                    {
                        data = (object)null,
                        errorMessage = "Company ID is required."
                    })));
                }

                var costCenters = _repository.GetCostCentersForCompany(companyId);

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = costCenters,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Failed to load company cost centers.",
                    errorDetails = ex.Message
                })));
            }
        }

        /// <summary>
        /// Get company details
        /// GET /api/costcenters/companydetails?companyId=DISCO4
        /// </summary>
        [HttpGet]
        [Route("companydetails")]
        public IHttpActionResult GetCompanyDetails(string companyId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                    {
                        data = (object)null,
                        errorMessage = "Company ID is required."
                    })));
                }

                var company = _repository.GetCompanyDetails(companyId);

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = company,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Failed to load company details.",
                    errorDetails = ex.Message
                })));
            }
        }
    }
}
