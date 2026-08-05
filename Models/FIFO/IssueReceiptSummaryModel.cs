using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.Accounts
{
    public class IssueReceiptSummaryModel
    {
        public string Category { get; set; }
        public string DocNo { get; set; }
        public DateTime? TrxDt { get; set; }
        public decimal? TrxVal { get; set; }
        public string TranStatus { get; set; }
        public string CctName { get; set; }
    }
}