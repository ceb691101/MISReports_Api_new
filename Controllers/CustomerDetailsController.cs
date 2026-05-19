using System;
using System.Web.Http;
using MISReports_Api.DAL.CustomerDetails;
using MISReports_Api.Models.CustomerDetails;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/customerdetails")]
    public class CustomerDetailsController : ApiController
    {
        private readonly PaymentInquiryDao _paymentInquiryDao;

        public CustomerDetailsController()
        {
            _paymentInquiryDao = new PaymentInquiryDao();
        }

        /// <summary>GET api/customerdetails/latest-update-times</summary>
        [HttpGet]
        [Route("latest-update-times")]
        public IHttpActionResult GetLatestUpdateTimes()
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetLatestUpdateTimes();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get latest update times.", errorDetails = ex.Message });
            }
        }

        /// <summary>POST api/customerdetails/payment-full-report</summary>
        [HttpPost]
        [Route("payment-full-report")]
        public IHttpActionResult GetPaymentFullReport([FromBody] PaymentInquiryRequest request)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetFullReport(request);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get payment full report.", errorDetails = ex.Message });
            }
        }

    }
}
