using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.Admin.Report_Parameters
{
    public class RepParaModel
    {
        public string ParaId { get; set; }
        public string ParaDesc { get; set; }
    }

    public class PopulateParamTsModel
    {
        public string RepId { get; set; }
        public string ParamList { get; set; }
    }

    public class SaveReportParamsResultModel
    {
        public string ParaName { get; set; }
        public bool Found { get; set; }
        public bool Inserted { get; set; }
        public bool Updated { get; set; }
    }

    public class PopulateParamTsResultModel
    {
        public int UpdatedRows { get; set; }
    }
}