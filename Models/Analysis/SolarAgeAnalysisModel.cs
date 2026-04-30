using System.Collections.Generic;

namespace MISReports_Api.Models.Analysis
{
    public class SolarAgeBillCycleModel
    {
        public string BillCycle { get; set; }
        public string BillMnth { get; set; }
    }

    public class SolarAgeCustomerModel
    {
        public string AccountNumber { get; set; }
        public string NetTypeCode { get; set; }
        public string NetType { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string AgreementDate { get; set; }
        public long AgeDays { get; set; }
    }

    public class SolarAgeAnalysisRequest
    {
        public string AreaCode { get; set; }
        public string BillCycle { get; set; }
        public string AgeBand { get; set; }
    }

    public class SolarAgeBillCycleResult
    {
        public string AreaCode { get; set; }
        public string MaxBillCycle { get; set; }
        public List<SolarAgeBillCycleModel> BillCycles { get; set; } = new List<SolarAgeBillCycleModel>();
        public string ErrorMessage { get; set; }
    }

    public class SolarAgeAnalysisResult
    {
        public string AreaCode { get; set; }
        public string BillCycle { get; set; }
        public string AgeBand { get; set; }
        public List<SolarAgeCustomerModel> Records { get; set; } = new List<SolarAgeCustomerModel>();
        public Dictionary<string, int> AgeBandCounts { get; set; } = new Dictionary<string, int>();
        public string ErrorMessage { get; set; }
    }
}
