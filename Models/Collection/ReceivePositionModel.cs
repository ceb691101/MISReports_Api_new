using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.Collection
{
    // ── Main report model ──────────────────────────────────────────────────────
    public class ReceivablePositionModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }

        public string OpeningBalance { get; set; }
        public string MonthlyCharge { get; set; }
        public string Debits { get; set; }
        public string Credits { get; set; }
        public string UnderCharge { get; set; }
        public string OverCharge { get; set; }
        public string Payments { get; set; }
        public string ClosingBalance { get; set; }
        public string ClosingBalanceWithoutFinAcc { get; set; }
        public string AverageCharge { get; set; }
        public string NoOfMonthsInArrears { get; set; }
        public string NoOfMonthsInArrearsWithoutFinAcc { get; set; }

        public string BillCycle { get; set; }
        public string BillType { get; set; }
        public string ErrorMessage { get; set; }
    }

    // ── Request model ──────────────────────────────────────────────────────────
    public class ReceivablePositionRequest
    {
        public string BillCycle { get; set; }
        public string BillType { get; set; }   // "O" = Ordinary, "B" = Bulk
        public string AreaCode { get; set; }
        public string ProvCode { get; set; }
    }

    // ── Bill type model (returned by GET api/receivable-position/bill-types) ──
    public class ReceivablePositionBillTypeModel
    {
        public string BillType { get; set; }
        public string DisplayName { get; set; }
    }

    // ── Area lookup model (areas-by-province / areas-by-region) ───────────────
    public class ReceivablePositionAreaModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }
}