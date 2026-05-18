using System.Collections.Generic;

namespace MISReports_Api.Models.CustomerDetails
{
    public class LatestUpdateTimeRecord
    {
        public string Agent { get; set; }
        public string Center { get; set; }
        public string LastUpdate { get; set; }
        public string AgentName { get; set; }
        public string CenterName { get; set; }
    }

    public class LatestUpdateTimeResponse
    {
        public List<LatestUpdateTimeRecord> Records { get; set; }
        public string ErrorMessage { get; set; }
    }
}
