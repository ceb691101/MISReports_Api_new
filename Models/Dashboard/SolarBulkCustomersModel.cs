namespace MISReports_Api.Models.Dashboard
{
    public class SolarBulkCustomersCount
    {
        public int CustomersCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarBulkCustomersSummary
    {
        public int TotalCustomers { get; set; }
        public int NetType1Customers { get; set; }
        public int NetType2Customers { get; set; }
        public int NetType3Customers { get; set; }
        public int NetType4Customers { get; set; }
        public string ErrorMessage { get; set; }
    }
}