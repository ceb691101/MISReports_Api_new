using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// Models/IssuesRaisedForJobsModel.cs
namespace MISReports_Api.Models
{
    public class IssuesRaisedForJobsModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public int? NoOfIssues { get; set; }
        public decimal? Qty { get; set; }
    }
}