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

        #region POS Counter Collection Breakup

        /// <summary>GET api/customerdetails/pos-provinces</summary>
        [HttpGet]
        [Route("pos-provinces")]
        public IHttpActionResult GetProvinces()
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetProvinces();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get provinces.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/customerdetails/pos-areas?provCode={provCode}</summary>
        [HttpGet]
        [Route("pos-areas")]
        public IHttpActionResult GetAreas([FromUri] string provCode)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetAreas(provCode);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get areas.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/customerdetails/pos-counters?provCode={provCode}</summary>
        [HttpGet]
        [Route("pos-counters")]
        public IHttpActionResult GetCounters([FromUri] string provCode)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetCounters(provCode);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get counters.", errorDetails = ex.Message });
            }
        }

        /// <summary>POST api/customerdetails/pos-collection-breakup</summary>
        [HttpPost]
        [Route("pos-collection-breakup")]
        public IHttpActionResult GetPOSCollectionBreakup([FromBody] POSCounterCollectionRequest request)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetPOSCounterCollectionBreakup(request);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get POS counter collection breakup.", errorDetails = ex.Message });
            }
        }

        #endregion
    }
}
