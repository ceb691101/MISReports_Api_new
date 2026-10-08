using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.SolarJobs
{
    public class SolarOrdinaryCustomersGenerationCapacityModel
    {
        public string Division { get; set; }

        public int NoOfAccounts { get; set; }

        public decimal? GeneratedCapacity { get; set; }
    }
}