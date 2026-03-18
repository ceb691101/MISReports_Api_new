using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.Models.Dashboard;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        private readonly BulkCustomersDao _bulkCustomersDao = new BulkCustomersDao();
        private readonly SalesAndCollectionRangeDao _salesAndCollectionRangeDao = new SalesAndCollectionRangeDao();

        /// <summary>
        /// Get active customer count (cst_st='0')
        /// </summary>
        [HttpGet]
        [Route("customers/active-count")]
        public IHttpActionResult GetActiveCustomerCount()
        {
            try
            {
                if (!_bulkCustomersDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                int count = _bulkCustomersDao.GetActiveCustomerCount();

                return Ok(new
                {
                    data = new { activeCustomerCount = count },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get active customer count.",
                    errorDetails = ex.Message
                });
            }
        }

        /// <summary>
        /// Get sales and collection data for ordinary customers (bill_type='O')
        /// for the last 8 bill cycles derived from MAX(bill_cycle).
        /// </summary>
        [HttpGet]
        [Route("salesCollection/range/ordinary")]
        public IHttpActionResult GetOrdinarySalesAndCollection()
        {
            try
            {
                if (!_salesAndCollectionRangeDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                SalesAndCollectionRangeResult result = _salesAndCollectionRangeDao.GetSalesAndCollectionRange();

                return Ok(new
                {
                    data = new
                    {
                        maxBillCycle = result.MaxBillCycle,
                        records = result.OrdinaryData
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get ordinary sales and collection data.",
                    errorDetails = ex.Message
                });
            }
        }

        /// <summary>
        /// Get sales and collection data for bulk customers (bill_type='B')
        /// for the last 8 bill cycles derived from MAX(bill_cycle).
        /// </summary>
        [HttpGet]
        [Route("salesCollection/range/bulk")]
        public IHttpActionResult GetBulkSalesAndCollection()
        {
            try
            {
                if (!_salesAndCollectionRangeDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                SalesAndCollectionRangeResult result = _salesAndCollectionRangeDao.GetSalesAndCollectionRange();

                return Ok(new
                {
                    data = new
                    {
                        maxBillCycle = result.MaxBillCycle,
                        records = result.BulkData
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get bulk sales and collection data.",
                    errorDetails = ex.Message
                });
            }
        }
    }

}