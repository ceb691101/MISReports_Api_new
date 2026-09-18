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

    public class BankCountRecord
    {
        public string BankCode { get; set; }
        public string BranCode { get; set; }
        public string BankLabel { get; set; }
        public int Count { get; set; }
    }

    public class MnthBillRecord
    {
        public string RefId { get; set; }
        public string BankCode { get; set; }
        public string BranCode { get; set; }
        public string AcctNumber { get; set; }
        public string CustFname { get; set; }
        public string CustLname { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string BillCycle { get; set; }
        public string BillMon { get; set; }
        public string FrmDate { get; set; }
        public string ToDate { get; set; }
        public string KwhUnits { get; set; }
        public string KwhCharge { get; set; }
        public string Tax { get; set; }
        public string Fac { get; set; }
        public string Payments { get; set; }
        public string Debit { get; set; }
        public string Credit { get; set; }
        public string OpenBal { get; set; }
        public string CloseBal { get; set; }
        public string ProcDate { get; set; }
        public string ProcTime { get; set; }
        public string ReqstStat { get; set; }
        public string ReqstTime { get; set; }
        public string PaidAmount { get; set; }
        public string PaidDate { get; set; }
    }
}
