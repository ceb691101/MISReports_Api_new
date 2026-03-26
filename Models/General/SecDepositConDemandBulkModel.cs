using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MISReports_Api.Models.SolarInformation;

namespace MISReports_Api.Models.General
{
    public class SecDepositConDemandBulkModel
    {
        // Common fields for all report types
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string Address { get; set; } // Combined address_l1 and address_l2
        public string City { get; set; }
        public string Tariff { get; set; }

        // Numeric fields with formatted display
        public string ContractDemand { get; set; }      // cntr_dmnd - formatted with commas
        public string SecurityDeposit { get; set; }     // tot_sec_dep - formatted with commas and 2 decimals
        public string TotalKWOUnits { get; set; }       // tot_untskwo - formatted with commas
        public string TotalKWDUnits { get; set; }       // tot_untskwd - formatted with commas
        public string TotalKWPUnits { get; set; }       // tot_untskwp - formatted with commas
        public string KVA { get; set; }                  // tot_kva - formatted with commas
        public string MonthlyCharge { get; set; }        // tot_amt - formatted with commas and 2 decimals

        // Location fields (for Province reports)
        public string ProvinceCode { get; set; }
        public string AreaCode { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }

        // Internal fields
        public string BillCycle { get; set; }
        public string ErrorMessage { get; set; }

        // Raw numeric values for potential calculations
        public decimal RawContractDemand { get; set; }
        public decimal RawSecurityDeposit { get; set; }
        public decimal RawTotalKWOUnits { get; set; }
        public decimal RawTotalKWDUnits { get; set; }
        public decimal RawTotalKWPUnits { get; set; }
        public decimal RawKVA { get; set; }
        public decimal RawMonthlyCharge { get; set; }
    }

    /// <summary>
    /// Request model for Security Deposit & Contract Demand Bulk reports
    /// </summary>
    public class SecDepositConDemandRequest
    {
        public string BillCycle { get; set; }        // bill_cycle parameter for mon_tot
        public SolarReportType ReportType { get; set; }

        // Location filters based on report type
        public string AreaCode { get; set; }    // For Area reports
        public string ProvCode { get; set; }    // For Province reports
        public string Region { get; set; }      // For Region reports (if needed)
    }

    // Helper class for province report data
    public class ProvinceAreaInfo
    {
        public string AccountNumber { get; set; }
        public string AreaCode { get; set; }
        public string ProvinceCode { get; set; }
        public string AreaName { get; set; }
        public string ProvinceName { get; set; }
    }
}