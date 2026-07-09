using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web.Hosting;

namespace MISReports_Api.Services.Reporting
{
    public class SolarPendingJobsRdlcReportService
    {
        /// <summary>
        /// Generates a PDF from the SolarPendingJobs.rdlc report using a DataTable.
        /// </summary>
        public byte[] GeneratePendingJobsPdf(DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                throw new Exception("No data supplied to the report.");

            // 1. Locate the RDLC file
            string reportPath = HostingEnvironment.MapPath("~/RdlcReports/SolarPendingJobs.rdlc");
            if (string.IsNullOrEmpty(reportPath) || !File.Exists(reportPath))
            {
                throw new FileNotFoundException(
                    "RDLC report file not found. Make sure SolarPendingJobs.rdlc is in the RdlcReports folder.");
            }

            // 2. Create the LocalReport instance
            LocalReport localReport = new LocalReport
            {
                ReportPath = reportPath
            };

            // 3. Bind the DataTable — "JobsDataSet" must match the dataset name in the RDLC
            ReportDataSource reportDataSource = new ReportDataSource("JobsDataSet", dataTable);
            localReport.DataSources.Add(reportDataSource);

            // 4. Set page layout
            string deviceInfo =
                @"<DeviceInfo>
                    <OutputFormat>PDF</OutputFormat>
                    <PageWidth>11in</PageWidth>
                    <PageHeight>8.5in</PageHeight>
                    <MarginTop>0.5in</MarginTop>
                    <MarginLeft>0.5in</MarginLeft>
                    <MarginRight>0.5in</MarginRight>
                    <MarginBottom>0.5in</MarginBottom>
                </DeviceInfo>";

            // 5. Render to PDF
            string mimeType, encoding, fileNameExtension;
            Warning[] warnings;
            string[] streams;

            byte[] pdfBytes = localReport.Render(
                "PDF",
                deviceInfo,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warnings);

            return pdfBytes;
        }

        /// <summary>
        /// Helper: Converts a list of values into a DataTable matching the RDLC dataset columns.
        /// Call this from your controller after fetching data from the database.
        /// </summary>
        public DataTable CreateDataTable(List<Dictionary<string, object>> rows)
        {
            DataTable dt = new DataTable("SolarPendingJobsTable");

            // Define columns — these must match the column names in your .xsd / RDLC dataset
            dt.Columns.Add("ApplicationNo", typeof(string));
            dt.Columns.Add("ProjectNo", typeof(string));
            dt.Columns.Add("SubmitDate", typeof(string));
            dt.Columns.Add("Status", typeof(string));

            foreach (var row in rows)
            {
                DataRow dr = dt.NewRow();
                dr["ApplicationNo"] = row.ContainsKey("APPLICATION_NO") ? row["APPLICATION_NO"] : "";
                dr["ProjectNo"] = row.ContainsKey("PROJECTNO") ? row["PROJECTNO"] : "";
                dr["SubmitDate"] = row.ContainsKey("SUBMIT_DATE") ? row["SUBMIT_DATE"] : "";
                dr["Status"] = row.ContainsKey("STATUS") ? row["STATUS"] : "";
                dt.Rows.Add(dr);
            }

            return dt;
        }
    }
}