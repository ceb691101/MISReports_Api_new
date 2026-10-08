using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.Accounts
{
    public class CCJobSummaryModel
    {
        public string JobNum { get; set; }
        public string MatNm { get; set; }
        public decimal? QtyOnHand { get; set; }
        public string MatCd { get; set; }
        public decimal? UnitCost { get; set; }
        public string TrxType { get; set; }
        public decimal? TrxQty { get; set; }
    }
}