using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.General
{
    // ── Main report row model ──────────────────────────────────────────────────
    public class ArrearsPositionModel
    {
        /// <summary>Reader code from prn_dat_1</summary>
        public string ReaderCode { get; set; }

        /// <summary>Formatted charge = sum(kwh_charge + fuel_charge) – sum(NR transactions)</summary>
        public string Charge { get; set; }

        /// <summary>Formatted current balance = sum(crnt_balance)</summary>
        public string CurrentBalance { get; set; }

        /// <summary>Formatted ratio = crnt_balance / charge (or 0 when charge = 0)</summary>
        public string Ratio { get; set; }

        /// <summary>Formatted count of accounts under this reader</summary>
        public string ReaderCount { get; set; }

        // ── Raw numeric values (used for summary totals) ──────────────────────
        public decimal RawCharge { get; set; }
        public decimal RawCurrentBalance { get; set; }
        public decimal RawRatio { get; set; }
        public int RawReaderCount { get; set; }

        public string ErrorMessage { get; set; }
    }

    // ── Request model ──────────────────────────────────────────────────────────
    public class ArrearsPositionRequest
    {
        public string BillCycle { get; set; }
        public string AreaCode { get; set; }
    }

    // ── Summary totals model ───────────────────────────────────────────────────
    public class ArrearsPositionSummary
    {
        public int TotalRecords { get; set; }
        public decimal TotalCharge { get; set; }
        public decimal TotalCurrentBalance { get; set; }
        public decimal TotalReaderCount { get; set; }
    }

    // ── Response wrapper ───────────────────────────────────────────────────────
    public class ArrearsPositionResponse
    {
        public string AreaCode { get; set; }
        public string BillCycle { get; set; }
        public List<ArrearsPositionModel> Data { get; set; }
        public ArrearsPositionSummary Summary { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}