using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.Accounts
{
    public class IssueSummaryProvinceModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }

        // c8 in the source query -- the company name of the department that owns the row
        // (correlated to d.comp_id / T1.dept_id), as opposed to CompName below which is the
        // top-level company requested via costctr/compId.
        public string DeptCompName { get; set; }

        public string DeptId { get; set; }

        // Aliased "commited_cost" in the source query, but the underlying SUM is actually
        // over trx_qty (transaction quantity), not a cost/amount value. Kept the property
        // name aligned to the SQL alias for traceability; treat this as a quantity.
        public decimal? CommitedQty { get; set; }

        public string CompName { get; set; }
    }
}