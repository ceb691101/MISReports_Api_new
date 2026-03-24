using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MISReports_Api.Models.SolarInformation;

namespace MISReports_Api.Models.General
{
    // ── Main report model ──────────────────────────────────────────────────────
    public class SecDepositConDemandBulkModel
    {
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Tariff { get; set; }

        public string ContractDemand { get; set; }
        public string SecurityDeposit { get; set; }
        public string TotalKWOUnits { get; set; }
        public string TotalKWDUnits { get; set; }
        public string TotalKWPUnits { get; set; }
        public string KVA { get; set; }
        public string MonthlyCharge { get; set; }

        public string ProvinceCode { get; set; }
        public string AreaCode { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }

        public string BillCycle { get; set; }
        public string ErrorMessage { get; set; }

        public decimal RawContractDemand { get; set; }
        public decimal RawSecurityDeposit { get; set; }
        public decimal RawTotalKWOUnits { get; set; }
        public decimal RawTotalKWDUnits { get; set; }
        public decimal RawTotalKWPUnits { get; set; }
        public decimal RawKVA { get; set; }
        public decimal RawMonthlyCharge { get; set; }
    }

    // ── Request model ──────────────────────────────────────────────────────────
    public class SecDepositConDemandRequest
    {
        public string BillCycle { get; set; }
        public SolarReportType ReportType { get; set; }
        public string AreaCode { get; set; }
        public string ProvCode { get; set; }
        public string Region { get; set; }
    }

    // ── Area model  (returned by GET api/contract-demand/areas) ───────────────
    public class AreaModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }

    // ── Province model  (returned by GET api/contract-demand/provinces) ───────
    public class ProvinceModel
    {
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
    }

    // ── Helper (used internally by province queries) ───────────────────────────
    public class ProvinceAreaInfo
    {
        public string AccountNumber { get; set; }
        public string AreaCode { get; set; }
        public string ProvinceCode { get; set; }
        public string AreaName { get; set; }
        public string ProvinceName { get; set; }
    }
}