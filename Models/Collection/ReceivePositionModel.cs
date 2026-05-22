using System.Collections.Generic;

namespace MISReports_Api.Models.CollectionInformation
{
    public class ReceivePositionModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public string BillCycle { get; set; }
        public string BillType { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal MonthlyCharge { get; set; }
        public decimal Debits { get; set; }
        public decimal Credits { get; set; }
        public decimal UnderCharge { get; set; }
        public decimal OverCharge { get; set; }
        public decimal Payments { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal ClosingBalanceWithoutFinAcc { get; set; }
        public decimal AverageCharge { get; set; }
        public decimal NoOfMonthsInArrears { get; set; }
        public decimal NoOfMonthsInArrearsWithoutFinAcc { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class ReceivePositionRequest
    {
        public string BillCycle { get; set; }
        public string BillType { get; set; }   // "O" or "B"
        public string AreaCode { get; set; }
    }

    public class AreaOption
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }

    // NEW: represents one row from prov_servers
    public class ProvinceOption
    {
        public string ProvCode { get; set; }
        public string ProvName { get; set; }
    }

    public class ReceivePositionDropdowns
    {
        public List<string> BillCycles { get; set; }
        public List<string> BillTypes { get; set; }
        public List<AreaOption> Areas { get; set; }

        // NEW: province list populated from prov_servers
        public List<ProvinceOption> Provinces { get; set; }
    }
}