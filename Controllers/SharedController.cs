using MISReports_Api.DAL.SolarInformation.SolarProgressClarification;
using MISReports_Api.DAL.SolarInformation.SolarPVConnections;
using MISReports_Api.DAL.SolarInformation.SolarPaymentRetail;
using MISReports_Api.DAL.SolarInformation.SolarPVCapacity;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff;
using MISReports_Api.DAL.Shared;
using MISReports_Api.DAL;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api")]
    public class SharedController : ApiController
    {
        private readonly AreasDao _areasDao = new AreasDao();
        private readonly ProvinceDao _provinceDao = new ProvinceDao();
        private readonly RegionDao _regionDao = new RegionDao();
        private readonly BillCycleDao _billCycleDao = new BillCycleDao();
        private readonly PVBillCycleDao _pvBillCycleDao = new PVBillCycleDao();
        private readonly ProvinceOrdinaryDao _provinceOrdinaryDao = new ProvinceOrdinaryDao();
        private readonly RegionOrdinaryDao _regionOrdinaryDao = new RegionOrdinaryDao();
        private readonly BillCycleOrdinaryDao _billCycleOrdinaryDao = new BillCycleOrdinaryDao();
        private readonly BillCycleRetailDao _billCycleRetailDao = new BillCycleRetailDao();
        private readonly AreasRepository _areasRepository = new AreasRepository();
        private readonly PVCapacityBillCycleDao _pVCapacityBillCycleDao = new PVCapacityBillCycleDao();
        private readonly BillCycleFromAreaDao _billCycleFromAreaDao = new BillCycleFromAreaDao();
        private readonly ContractDemandBillCycleDao _contractDemandBillCycle = new ContractDemandBillCycleDao();
        private readonly ActiveCustSalesOrdBillCycleDao _activeCustSalesOrdBillCycle = new ActiveCustSalesOrdBillCycleDao();
        private readonly ActiveCustSalesBulkBillCycleDao _activeCustSalesBulkBillCycle = new ActiveCustSalesBulkBillCycleDao();

        [HttpGet]
        [Route("ordinary/areas")]
        public IHttpActionResult GetAreas()
        {
            try
            {
                if (!_areasDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var areas = _areasRepository.GetAreas();

                return Ok(JObject.FromObject(new
                {
                    data = areas,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas data.",
                    errorDetails = ex.Message
                }));
            }
        }


        [HttpGet]
        [Route("bulk/areas")]
        public IHttpActionResult GetBulkAreas()
        {
            try
            {
                if (!_areasDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var areas = _areasDao.GetAreas();

                return Ok(JObject.FromObject(new
                {
                    data = areas,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/province")]
        public IHttpActionResult GetProvince()
        {
            try
            {
                if (!_provinceDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var province = _provinceDao.GetProvince();

                return Ok(JObject.FromObject(new
                {
                    data = province,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get province data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/province")]
        public IHttpActionResult GetOrdinaryProvince()
        {
            try
            {
                if (!_provinceOrdinaryDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var province = _provinceOrdinaryDao.GetProvince();

                return Ok(JObject.FromObject(new
                {
                    data = province,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get province data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/region")]
        public IHttpActionResult GetRegion()
        {
            try
            {
                if (!_regionDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var region = _regionDao.GetRegion();

                return Ok(JObject.FromObject(new
                {
                    data = region,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get region data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/region")]
        public IHttpActionResult GetOrdinaryRegion()
        {
            try
            {
                if (!_regionOrdinaryDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var region = _regionOrdinaryDao.GetRegion();

                return Ok(JObject.FromObject(new
                {
                    data = region,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get region data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/netmtchg/billcycle/max")]  //billcycle/max
        public IHttpActionResult GetMaxBillCycle()
        {
            try
            {
                var result = _billCycleDao.GetLast24BillCycles();//From netmtchg table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/netmtcons/billcycle/max")] //bill-cycle
        public IHttpActionResult GetPVBillCycle()
        {
            try
            {
                var result = _pvBillCycleDao.GetLast24BillCycles();//From netmtcons table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/netmtchg/billcycle/max")] //ordinary/bill-cycle
        public IHttpActionResult GetOrdinaryBillCycle()
        {
            try
            {
                var result = _billCycleOrdinaryDao.GetLast24BillCycles();//From netmtchg table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/netmtcons/billcycle/max")] // retail/billcycle
        public IHttpActionResult GetRetailBillCycle()
        {
            try
            {
                var result = _billCycleRetailDao.GetLast24BillCycles();//From netmtcons table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/netprogrs/billcycle/max")] //solarPVCapacity/billcycle/max
        public IHttpActionResult GetPVCapacityMaxBillCycle()
        {
            try
            {
                var result = _pVCapacityBillCycleDao.GetLast24BillCycles();//From netprogrs table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("areas/billcycle/max")] //areas/billcycle/max
        public IHttpActionResult GetBillCycleFromArea()
        {
            try
            {
                var result = _billCycleFromAreaDao.GetLast24BillCycles();//From areas table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle from areas",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/mon_tot/billcycle/max")] //ContractDemandBulk
        public IHttpActionResult GetContractDemandBillCycle()
        {
            try
            {
                var result = _contractDemandBillCycle.GetLast24BillCycles();//From mon_tot table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/consmry/billcycle/max")] //Active Customers and Sales by Tariff
        public IHttpActionResult GetActiveCustomersSalesOrdBillCycle()
        {
            try
            {
                var result = _activeCustSalesOrdBillCycle.GetLast36BillCycles();//From consmry table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/account_info/billcycle/max")] //Active Customers and Sales by Tariff
        public IHttpActionResult GetActiveCustomersSalesBulkBillCycle()
        {
            try
            {
                var result = _activeCustSalesBulkBillCycle.GetLast36BillCycles();//From account_info table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}