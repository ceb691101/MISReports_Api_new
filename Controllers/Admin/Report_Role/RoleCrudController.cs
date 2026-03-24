using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/rolecrud")]
    public class RoleCrudController : ApiController
    {
        private readonly RoleCrudRepository _repo = new RoleCrudRepository();

        [HttpPost]
        [Route("user")]
        public IHttpActionResult CreateUser([FromBody] RoleCrudRequest req)
        {
            var rows = _repo.InsertUser(req.RoleId, req.RoleName, req.Password, req.UserType, req.Company, req.CompSub);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }

        [HttpPut]
        [Route("user/{roleId}")]
        public IHttpActionResult UpdateUser(string roleId, [FromBody] RoleCrudRequest req)
        {
            var rows = _repo.UpdateUser(roleId, req.RoleName, req.Password, req.UserType, req.Company, req.CompSub);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }

        [HttpDelete]
        [Route("user/{roleId}")]
        public IHttpActionResult DeleteUser(string roleId)
        {
            var rows = _repo.DeleteUserBasic(roleId);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }

        [HttpDelete]
        [Route("user/{roleId}/type/{type}")]
        public IHttpActionResult DeleteUserByType(string roleId, string type)
        {
            var rows = _repo.DeleteUserByType(roleId, type);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }

        [HttpPost]
        [Route("costcentre")]
        public IHttpActionResult SaveRoleCct([FromBody] RoleCostCentreRequest req)
        {
            var exists = _repo.ExistsRoleCostCentre(req.RoleId, req.CostCentre);
            var rows = exists
                ? _repo.UpdateCostCentreLevel(req.RoleId, req.CostCentre, req.LvlNo)
                : _repo.InsertUserCostCentre(req.RoleId, req.CostCentre, req.LvlNo);

            return Ok(JObject.FromObject(new { data = new { affectedRows = rows, operation = exists ? "UPDATE" : "INSERT" }, errorMessage = (string)null }));
        }

        [HttpDelete]
        [Route("user/{roleId}/reports")]
        public IHttpActionResult DeleteAllReports(string roleId)
        {
            var rows = _repo.DeleteAllReports(roleId);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }

        [HttpDelete]
        [Route("user/{roleId}/reports/{repId}")]
        public IHttpActionResult DeleteSpecificReport(string roleId, string repId)
        {
            var rows = _repo.DeleteSpecificReport(roleId, repId);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }

        [HttpPost]
        [Route("user/{roleId}/reports/delete-by-category")]
        public IHttpActionResult DeleteByCategory(string roleId, [FromBody] List<string> catCodes)
        {
            var rows = _repo.DeleteReportsByCategory(roleId, catCodes);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }

        [HttpDelete]
        [Route("user/{roleId}/costcentre/{cct}")]
        public IHttpActionResult DeleteCostCentre(string roleId, string cct)
        {
            var rows = _repo.DeleteCostCentre(roleId, cct);
            return Ok(JObject.FromObject(new { data = new { affectedRows = rows }, errorMessage = (string)null }));
        }
    }
}