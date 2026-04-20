using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.General.ListOfGovernmentAccounts;
using MISReports_Api.DAL.General.AreasPosition;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.DAL.Shared;
using MISReports_Api.Models.SolarInformation;
using MISReports_Api.Models.General;
using MISReports_Api.Models.Shared;
using MISReports_Api.Models;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        private readonly ContractDemandBulkDao _contractDemandBulkDao = new ContractDemandBulkDao();
        private readonly ContractDemandBillCycleDao _billCycleDao = new ContractDemandBillCycleDao();
        private readonly SalesAndCollectionRangeDao _dao = new SalesAndCollectionRangeDao();
        private readonly RegisteredCustomersBillCycleDao _smsDao = new RegisteredCustomersBillCycleDao();
        private readonly AreasDao _areasDao = new AreasDao();
        private readonly ProvinceDao _provinceDao = new ProvinceDao();
        private readonly ListOfGovernmentAccountsDao _govAccountsDao = new ListOfGovernmentAccountsDao();
        private readonly AreasPositionDao _areasPositionDao = new AreasPositionDao();   // ← new

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
        // Areas                                                                //
        // GET api/shared/areas                                                 //
        // Response: { data: [ { areaCode, areaName }, ... ],                 //
        //             errorMessage: null }                                     //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("shared/areas")]
        public IHttpActionResult GetAreas()
        {
            try
            {
                if (!_areasDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _areasDao.GetAreas();

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No areas found.",
                        errorDetails = "The areas table may be empty."
                    });

                return Ok(new { data = data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetAreas: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve areas.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // Provinces                                                            //
        // GET api/shared/provinces                                             //
        // Response: { data: [ { provinceCode, provinceName }, ... ],         //
        //             errorMessage: null }                                     //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("shared/provinces")]
        public IHttpActionResult GetProvinces()
        {
            try
            {
                if (!_provinceDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _provinceDao.GetProvince();

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No provinces found.",
                        errorDetails = "The provinces table may be empty."
                    });

                return Ok(new { data = data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetProvinces: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve provinces.",
                    errorDetails = ex.Message
                });
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
                    data = data,   // List<SecDepositConDemandBulkModel>
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
        // SMS Registered Range                                                 //
        // GET api/original/smsRegisteredRange                                 //
        //     ?fromCycle=420&toCycle=438&reportType=area&typeCode=43          //
        // Response: { data: { locationName, monthlyCounts }, errorMessage }   //
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

        // ------------------------------------------------------------------ //
        // Government Accounts - Max Bill Cycle                               //
        // GET api/government-accounts/max-bill-cycle?areaCode=43             //
        // Response: { data: { maxBillCycle: "438" }, errorMessage: null }    //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/max-bill-cycle")]
        public IHttpActionResult GetMaxBillCycle([FromUri] string areaCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(new { data = (object)null, errorMessage = "Area code is required." });

                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var result = _govAccountsDao.GetMaxBillCycle(areaCode);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    return Ok(new { data = (object)null, errorMessage = result.ErrorMessage });

                if (string.IsNullOrEmpty(result.MaxBillCycle))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No bill cycles found for the selected area.",
                        errorDetails = "The area may have no billing data."
                    });

                return Ok(new { data = result, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR GetMaxBillCycle: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve max bill cycle.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // Government Accounts - Departments                                  //
        // GET api/government-accounts/departments                           //
        // Response: { data: [ { departmentCode, departmentName }, ... ],    //
        //             errorMessage: null }                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/departments")]
        public IHttpActionResult GetDepartments()
        {
            try
            {
                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _govAccountsDao.GetDepartments();

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No departments found.",
                        errorDetails = "The department table may be empty."
                    });

                return Ok(new { data = data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR GetDepartments: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve departments.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // Government Accounts - Area Report                                  //
        // GET api/government-accounts/area?billCycle=438&areaCode=43        //
        // Response: { data: [ ...GovernmentAccountRecords... ],             //
        //             errorMessage: null }                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/area")]
        public IHttpActionResult GetGovernmentAccountsAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessGovernmentAccountsRequest(new GovernmentAccountRequest
            {
                BillCycle = billCycle,
                ReportType = "area",
                AreaCode = areaCode
            });
        }

        // ------------------------------------------------------------------ //
        // Government Accounts - Department Report                           //
        // GET api/government-accounts/department?billCycle=438&areaCode=43  //
        //     &departmentCode=ABC                                           //
        // Response: { data: [ ...GovernmentAccountRecords... ],             //
        //             errorMessage: null }                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/department")]
        public IHttpActionResult GetGovernmentAccountsDepartmentReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null,
            [FromUri] string departmentCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");
            if (string.IsNullOrWhiteSpace(departmentCode)) errors.Add("Department code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessGovernmentAccountsRequest(new GovernmentAccountRequest
            {
                BillCycle = billCycle,
                ReportType = "department",
                AreaCode = areaCode,
                DepartmentCode = departmentCode
            });
        }

        // ------------------------------------------------------------------ //
        // Shared government accounts report processor                        //
        // IMPORTANT: Returns data as a flat List<> directly under "data".   //
        // ------------------------------------------------------------------ //

        private IHttpActionResult ProcessGovernmentAccountsRequest(GovernmentAccountRequest request)
        {
            try
            {
                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                // Returns List<ListOfGovernmentAccountsModel>
                var data = _govAccountsDao.GetGovernmentAccountsReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle, area code, and department code."
                    });

                // Return flat array directly
                return Ok(new
                {
                    data = data,   // List<ListOfGovernmentAccountsModel>
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessGovernmentAccountsRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get government accounts report data.",
                    errorDetails = ex.Message
                });
            }
        }

        // ================================================================== //
        //  AREAS POSITION                                                      //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // Areas Position — Max Bill Cycle                                     //
        // GET api/areas-position/max-bill-cycle?areaCode=43                  //
        // Response: { data: { billCycle: "438" }, errorMessage: null }       //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("areas-position/max-bill-cycle")]
        public IHttpActionResult GetAreasPositionMaxBillCycle([FromUri] string areaCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(new { data = (object)null, errorMessage = "Area code is required." });

                if (!_areasPositionDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var billCycle = _areasPositionDao.GetMaxBillCycle(areaCode);

                if (string.IsNullOrEmpty(billCycle))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No bill cycle found for the selected area.",
                        errorDetails = "The area may have no billing data."
                    });

                return Ok(new
                {
                    data = new { billCycle },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetAreasPositionMaxBillCycle: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve max bill cycle.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // Areas Position — Report                                             //
        // GET api/areas-position/report?areaCode=43                          //
        //     (optional) &billCycle=438  — omit to auto-resolve max cycle    //
        //                                                                     //
        // Response: {                                                         //
        //   data: {                                                           //
        //     billCycle: "438",                                               //
        //     rows: [                                                         //
        //       {                                                             //
        //         readerCode,                                                 //
        //         monthlyBill,                                                //
        //         totalBalance,                                               //
        //         noOfMonthsInArrears,                                       //
        //         noOfAccounts                                                //
        //       }, ...                                                        //
        //     ]                                                               //
        //   },                                                                //
        //   errorMessage: null                                                //
        // }                                                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("areas-position/report")]
        public IHttpActionResult GetAreasPositionReport(
            [FromUri] string areaCode = null,
            [FromUri] string billCycle = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(new { data = (object)null, errorMessage = "Area code is required." });

                if (!_areasPositionDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var result = _areasPositionDao.GetAreasPositionReport(new AreasPositionRequest
                {
                    AreaCode = areaCode,
                    BillCycle = billCycle   // null/empty → DAO resolves max automatically
                });

                if (result == null || result.Rows == null || result.Rows.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected area.",
                        errorDetails = "Please check the area code or billing data availability."
                    });

                return Ok(new
                {
                    data = new
                    {
                        billCycle = result.BillCycle,
                        rows = result.Rows
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetAreasPositionReport: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve areas position report.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}