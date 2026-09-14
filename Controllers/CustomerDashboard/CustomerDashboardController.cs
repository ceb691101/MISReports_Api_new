using System;
using System.Web.Http;
using MISReports_Api.DAL.CustomerDashboard;
using MISReports_Api.Models.CustomerDashboard;

namespace MISReports_Api.Controllers.CustomerDashboard
{
    [RoutePrefix("api/CustomerDashboard")]
    public class CustomerDashboardController : ApiController
    {
        private readonly CustomerDetailDao _customerDetailDao;

        public CustomerDashboardController()
        {
            _customerDetailDao = new CustomerDetailDao();
        }

        /// <summary>
        /// GET api/CustomerDashboard/GetCustomerDetails?accNumber=...
        /// Returns customer details (acc_number, cust_fname) from table mnth_bill in stndordr database.
        /// </summary>
        [HttpGet]
        [Route("GetCustomerDetails")]
        public IHttpActionResult GetCustomerDetails([FromUri] string accNumber = null)
        {
            try
            {
                var response = _customerDetailDao.GetCustomerDetails(accNumber);

                if (!response.Success)
                {
                    return Ok(new
                    {
                        success = false,
                        data = (object)null,
                        errorMessage = response.ErrorMessage
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = response.Records,
                    count = response.Records.Count,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    data = (object)null,
                    errorMessage = ex.Message
                });
            }
        }
    }
}
