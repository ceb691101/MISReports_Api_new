using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
// Import the Database Access Layer namespace (e.g., MISReports_Api_new.DAL or DBAccess)

namespace MISReports_Api_new.Controllers
{
    [RoutePrefix("api")]
    public class SystemController : ApiController
    {
        [HttpGet]
        [Route("user-systems")]
        public IHttpActionResult GetUserSystems()
        {
            try
            {
                // SQL query to fetch system names and URLs from the SYSTEM_URL table
                string query = "SELECT TRIM(SYSTEM) AS SYSTEM_NAME, TRIM(URL) AS SYSTEM_URL FROM SYSTEM_URL";

                // Execute query via Data Access Layer
                DataTable dt = DBAccess.ExecuteQuery(query);

                var list = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new
                    {
                        name = row["SYSTEM_NAME"].ToString(),
                        url = row["SYSTEM_URL"].ToString()
                    });
                }

                return Ok(new { success = true, systems = list });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}