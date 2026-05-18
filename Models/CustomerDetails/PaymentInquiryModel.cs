using System.Collections.Generic;

namespace MISReports_Api.Models.CustomerDetails
{
    public class PaymentInquiryRequest
    {
        public string AcctNo { get; set; }
        public string FromDate { get; set; }
    }

    public class LatestUpdateTimeRecord
    {
        public string Agent { get; set; }
        public string Center { get; set; }
        public string LastUpdate { get; set; }
        public string AgentName { get; set; }
        public string CenterName { get; set; }
    }

    public class PaymentInquiryPaymentRecord
    {
        public string TransDate { get; set; }
        public string TransAmt { get; set; }
        public string Center { get; set; }
        public string CountNo { get; set; }
        public string PayMode { get; set; }
        public string TransTime { get; set; }
        public string TransType { get; set; }
        public string StubNo { get; set; }
        public string Agent { get; set; }
        public string UsrLot { get; set; }
        public string AgentName { get; set; }
        public string CenterName { get; set; }
        public string CounterName { get; set; }
        public string CodeDescription { get; set; }
        public string ChequeMoneyOrderNo { get; set; }
    }

    public class PaymentInquiryResponse
    {
        public string AccountNumber { get; set; }
        public string AreaName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerType { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PaymentInquiryPaymentRecord> PaymentRecords { get; set; } = new List<PaymentInquiryPaymentRecord>();
        public string ErrorMessage { get; set; }
    }

    public class LatestUpdateTimeResponse
    {
        public List<LatestUpdateTimeRecord> Records { get; set; }
        public string ErrorMessage { get; set; }
    }
}
