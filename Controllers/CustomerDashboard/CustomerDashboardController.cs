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
        /// GET api/CustomerDashboard?accNumber=...
        /// Returns standing order customer records (stod_cust joined with bank_name) where status1 = 'Q'.
        /// </summary>
        [HttpGet]
        [Route("")]
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

        /// <summary>
        /// GET api/CustomerDashboard/GetCustomerCount
        /// Returns total row count from stod_cust where status1 = 'Q'.
        /// </summary>
        [HttpGet]
        [Route("GetCustomerCount")]
        public IHttpActionResult GetCustomerCount()
        {
            try
            {
                int totalCount = _customerDetailDao.GetTotalCustomerCount();
                return Ok(new
                {
                    success = true,
                    totalCount = totalCount,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    totalCount = 0,
                    errorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// GET api/CustomerDashboard/GetCountByBank
        /// Returns count of stod_cust records (status1 = 'Q') grouped by each configured bank/branch.
        /// </summary>
        [HttpGet]
        [Route("GetCountByBank")]
        public IHttpActionResult GetCountByBank()
        {
            try
            {
                var records = _customerDetailDao.GetCountByBank();
                return Ok(new
                {
                    success = true,
                    data = records,
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

        /// <summary>
        /// GET api/CustomerDashboard/GetMnthBill?acctNumber=...
        /// Returns all mnth_bill records for the given account, ordered latest to oldest.
        /// </summary>
        [HttpGet]
        [Route("GetMnthBill")]
        public IHttpActionResult GetMnthBill([FromUri] string acctNumber)
        {
            if (string.IsNullOrWhiteSpace(acctNumber))
                return Ok(new { success = false, data = (object)null, errorMessage = "acctNumber is required." });

            try
            {
                var records = _customerDetailDao.GetMnthBillsByAccount(acctNumber);
                return Ok(new
                {
                    success = true,
                    data = records,
                    count = records.Count,
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
