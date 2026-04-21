using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.General
{
    public class ListOfGovernmentAccountsModel
    {
        // Raw database values
        public string AccountNumber { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public decimal RawCurrentBalance { get; set; }
        public decimal RawKwhCharge { get; set; }
        public decimal RawAverageConsumption { get; set; }

        // Formatted display values
        public string CurrentBalance { get; set; }
        public string KwhCharge { get; set; }
        public string AverageConsumption { get; set; }

        // Location information
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public string DepartmentCode { get; set; }
        public string DepartmentName { get; set; }
        public string BillCycle { get; set; }

        // Combined customer name
        public string CustomerName { get; set; }

        // Combined address
        public string Address { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class GovernmentAccountRequest
    {
        public string BillCycle { get; set; }
        public string ReportType { get; set; } // "area" or "department"
        public string AreaCode { get; set; }
        public string DepartmentCode { get; set; }
    }

    public class MaxBillCycleModel
    {
        public string MaxBillCycle { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DepartmentModel
    {
        public string DepartmentCode { get; set; }
        public string DepartmentName { get; set; }
    }
}