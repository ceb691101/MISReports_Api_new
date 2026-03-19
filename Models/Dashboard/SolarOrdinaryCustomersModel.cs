namespace MISReports_Api.Models.Dashboard
{
    public class SolarOrdinaryCustomersSummary
    {
        public string BillCycle { get; set; }
        public int TotalCustomers { get; set; }
        public int NetMeteringCustomers { get; set; }
        public int NetAccountingCustomers { get; set; }
        public int NetPlusCustomers { get; set; }
        public int NetPlusPlusCustomers { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarOrdinaryCustomersCount
    {
        public string BillCycle { get; set; }
        public int CustomersCount { get; set; }
        public string ErrorMessage { get; set; }
    }
}