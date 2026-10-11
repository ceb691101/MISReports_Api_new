using System.Collections.Generic;

namespace MISReports_Api.Models.SolarInformation
{
    public class SolarPaymentLissReconItem
    {
        public string NetType { get; set; }
        public string SchemeName { get; set; }

        // Ordinary breakdown
        public decimal OrdFixedPayment { get; set; }
        public decimal OrdOtherAmount { get; set; }
        public decimal OrdVariablePayment { get; set; }
        public decimal OrdTotalLiss { get; set; }
        public decimal OrdFinancialReport { get; set; }
        public decimal OrdDifference { get; set; }

        // Bulk breakdown
        public decimal BulkFixedPayment { get; set; }
        public decimal BulkOtherAmount { get; set; }
        public decimal BulkVariablePayment { get; set; }
        public decimal BulkTotalLiss { get; set; }
        public decimal BulkFinancialReport { get; set; }
        public decimal BulkDifference { get; set; }

        // Total (Ordinary + Bulk)
        public decimal TotalFixedPayment { get; set; }
        public decimal TotalOtherAmount { get; set; }
        public decimal TotalVariablePayment { get; set; }
        public decimal TotalLiss { get; set; }
        public decimal TotalFinancialReport { get; set; }
        public decimal TotalDifference { get; set; }
    }

    public class SolarPaymentLissReconResponse
    {
        public List<SolarPaymentLissReconItem> Rows { get; set; } = new List<SolarPaymentLissReconItem>();
        public SolarPaymentLissReconItem GrandTotal { get; set; }
        public string BillCycle { get; set; }
        public string ReportCategory { get; set; }
        public string TypeCode { get; set; }
    }

    public class SolarPaymentLissReconRequest
    {
        public string BillCycle { get; set; }
        public string ReportCategory { get; set; } // "Province" or "Division" / "Region"
        public string TypeCode { get; set; } // Province Code (e.g. "1") or Region Code (e.g. "R2")
    }
}
