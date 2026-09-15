using System.Collections.Generic;

namespace MISReports_Api.Models.CustomerDashboard
{
    public class CustomerDetailRecord
    {
        public string AcctNumber { get; set; }
        public string BillCycle { get; set; }
        public string Status1 { get; set; }
        public string BankCode { get; set; }
        public string BranCode { get; set; }
        public string BranName { get; set; }
        public string LastProcDate { get; set; }
        public string LastOutBal { get; set; }
        public string CalcCycle { get; set; }
        public string NewStat { get; set; }
        public string BranAdd1 { get; set; }
        public string BranAdd2 { get; set; }
        public string BranAdd3 { get; set; }
        public string BranTelno { get; set; }
        public string BranEmail { get; set; }
    }

    public class CustomerDetailResponse
    {
        public List<CustomerDetailRecord> Records { get; set; } = new List<CustomerDetailRecord>();
        public string ErrorMessage { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
}
