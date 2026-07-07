using MISReports_Api.Services.Reporting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/rdlc-reports")]
    public class RdlcReportController : ApiController
    {
        private readonly SolarPendingJobsRdlcReportService _reportService
            = new SolarPendingJobsRdlcReportService();

        [HttpGet]
        [Route("solar-pending-jobs")]
        public HttpResponseMessage DownloadSolarPendingJobsPdf()
        {
            try
            {
                // --- FOR TESTING: Use dummy data ---
                DataTable dt = GetTestData();

                // --- FOR PRODUCTION: Replace with your actual DAO call ---
                // var rows = yourDao.GetSolarPendingJobs(params);
                // DataTable dt = _reportService.CreateDataTable(rows);

                // Generate the PDF
                byte[] pdfBytes = _reportService.GeneratePendingJobsPdf(dt);

                // Return PDF as a downloadable file
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(pdfBytes)
                };
                response.Content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = $"SolarPendingJobs_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                    };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Temporary test data. Remove this once you connect your real database query.
        /// </summary>
        private DataTable GetTestData()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ApplicationNo", typeof(string));
            dt.Columns.Add("ProjectNo", typeof(string));
            dt.Columns.Add("SubmitDate", typeof(string));
            dt.Columns.Add("Status", typeof(string));

            dt.Rows.Add("APP-001", "PRJ-100", "2026-01-15", "Pending");
            dt.Rows.Add("APP-002", "PRJ-101", "2026-02-20", "Approved");
            dt.Rows.Add("APP-003", "PRJ-102", "2026-03-10", "Pending");

            return dt;
        }
    }
}