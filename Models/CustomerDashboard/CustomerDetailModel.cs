using System.Collections.Generic;

namespace MISReports_Api.Models.CustomerDashboard
{
    public class CustomerDetailRecord
    {
        public string AccNumber { get; set; }
        public string CustFname { get; set; }
    }

    public class CustomerDetailResponse
    {
        public List<CustomerDetailRecord> Records { get; set; } = new List<CustomerDetailRecord>();
        public string ErrorMessage { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
}
