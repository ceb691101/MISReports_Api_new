using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.Models.SolarInformation;
using MISReports_Api.Models.General;
using MISReports_Api.DAL;
using MISReports_Api.Models;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        private readonly ContractDemandBulkDao _contractDemandBulkDao = new ContractDemandBulkDao();
        private readonly ContractDemandBillCycleDao _billCycleDao = new ContractDemandBillCycleDao();
        private readonly ContractDemandAreaProvinceDao _areaProvinceDao = new ContractDemandAreaProvinceDao();
        private readonly SalesAndCollectionRangeDao _dao = new SalesAndCollectionRangeDao();
        private readonly RegisteredCustomersBillCycleDao _smsDao = new RegisteredCustomersBillCycleDao();

        // ------------------------------------------------------------------ //
        // Bill Cycles                                                          //
        // GET api/contract-demand/bill-cycles                                  //
        // Response: { data: { MaxBillCycle: "438",                            //
        //                     BillCycles: ["Jan 2026", "Dec 2025", ...] },    //
        //             errorMessage: null }                                     //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bill-cycles")]
        public IHttpActionResult GetBillCycles()
        {
            try
            {
                var model = _billCycleDao.GetLast24BillCycles();

                if (!string.IsNullOrEmpty(model.ErrorMessage))
                    return Ok(new { data = (object)null, errorMessage = model.ErrorMessage });

                return Ok(new { data = model, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve bill cycles.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // All Areas                                                            //
        // GET api/contract-demand/areas                                        //
        // Response: { data: [ { AreaCode, AreaName }, ... ], errorMessage }   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/areas")]
        public IHttpActionResult GetAreas()
        {
            try
            {
                if (!_areaProvinceDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var areas = _areaProvinceDao.GetAllAreas();

                if (areas == null || areas.Count == 0)
                    return Ok(new { data = (object)null, errorMessage = "No areas found in the database." });

                return Ok(new { data = areas, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot retrieve areas.", errorDetails = ex.Message });
            }
        }

        // ------------------------------------------------------------------ //
        // All Provinces                                                        //
        // GET api/contract-demand/provinces                                    //
        // Response: { data: [ { ProvinceCode, ProvinceName }, ... ],          //
        //             errorMessage: null }                                     //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/provinces")]
        public IHttpActionResult GetProvinces()
        {
            try
            {
                if (!_areaProvinceDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var provinces = _areaProvinceDao.GetAllProvinces();

                if (provinces == null || provinces.Count == 0)
                    return Ok(new { data = (object)null, errorMessage = "No provinces found in the database." });

                return Ok(new { data = provinces, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot retrieve provinces.", errorDetails = ex.Message });
            }
        }

        // ------------------------------------------------------------------ //
        // Contract Demand Bulk — Area                                          //
        // GET api/contract-demand/bulk/area?billCycle=438&areaCode=43         //
        // Response: { data: [ ...CustomerRecords... ], errorMessage: null }   //
        //                                                                      //
        // NOTE: data is a FLAT ARRAY — not a nested object.                   //
        //       Frontend reads: const records = json?.data ?? []              //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bulk/area")]
        public IHttpActionResult GetContractDemandAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessContractDemandRequest(new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Area,
                AreaCode = areaCode
            });
        }

        // ------------------------------------------------------------------ //
        // Contract Demand Bulk — Province                                      //
        // GET api/contract-demand/bulk/province?billCycle=438&provCode=D      //
        // Response: { data: [ ...CustomerRecords... ], errorMessage: null }   //
        //                                                                      //
        // NOTE: data is a FLAT ARRAY — not a nested object.                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bulk/province")]
        public IHttpActionResult GetContractDemandProvinceReport(
            [FromUri] string billCycle = null,
            [FromUri] string provCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(provCode)) errors.Add("Province code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessContractDemandRequest(new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Province,
                ProvCode = provCode
            });
        }

        // ------------------------------------------------------------------ //
        // Shared report processor                                              //
        // IMPORTANT: Returns data as a flat List<> directly under "data".     //
        //            Do NOT wrap it in { records, summary, title, ... }.      //
        //            The frontend maps json.data as CustomerRecord[].         //
        // ------------------------------------------------------------------ //

        private IHttpActionResult ProcessContractDemandRequest(SecDepositConDemandRequest request)
        {
            try
            {
                if (!_contractDemandBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                // Returns List<SecDepositConDemandBulkModel>
                var data = _contractDemandBulkDao.GetContractDemandBulkReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle and location code."
                    });

                // ✅ Return flat array directly — frontend does: json.data as CustomerRecord[]
                return Ok(new
                {
                    data = data,           // List<SecDepositConDemandBulkModel>
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessContractDemandRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // Sales Collection Range                                               //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("salesCollection/range")]
        public IHttpActionResult GetRange()
        {
            try
            {
                if (!_dao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                return Ok(new { data = _dao.GetSalesAndCollectionRange(), errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve sales and collection range data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // SMS Registered Range                                                 //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("original/smsRegisteredRange")]
        public IHttpActionResult GetSMSRegisteredRange(
            [FromUri] string fromCycle = null,
            [FromUri] string toCycle = null,
            [FromUri] string reportType = null,
            [FromUri] string typeCode = null)
        {
            try
            {
                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(fromCycle)) errors.Add("From bill cycle is required.");
                if (string.IsNullOrWhiteSpace(toCycle)) errors.Add("To bill cycle is required.");
                if (string.IsNullOrWhiteSpace(reportType)) errors.Add("Report type is required.");

                if (errors.Count > 0)
                    return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

                var monthlyData = _smsDao.GetSMSCountRange(new SMSUsageRequest
                {
                    FromBillCycle = fromCycle,
                    ToBillCycle = toCycle,
                    ReportType = reportType,
                    TypeCode = typeCode
                });

                if (monthlyData == null || monthlyData.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data available.",
                        errorDetails = "Please check the bill cycle range and location code."
                    });

                return Ok(new
                {
                    data = new Models.SMSRegisteredCustomersModel
                    {
                        LocationName = string.IsNullOrEmpty(typeCode) ? "Entire CEB" : typeCode,
                        MonthlyCounts = monthlyData
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetSMSRegisteredRange: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve SMS registered range data.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}
