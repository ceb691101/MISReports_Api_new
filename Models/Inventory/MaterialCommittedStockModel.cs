namespace MISReports_Api.Models
{
    public class MaterialCommittedStockModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public decimal CommittedCost { get; set; }
        public string DeptInfo { get; set; }
        public string Area { get; set; }
        public string UomCd { get; set; }
        public string Region { get; set; }
    }

    public class MaterialCommittedStockProvinceModel
    {
        public string CompId { get; set; }
        public string CompNm { get; set; }
    }
}