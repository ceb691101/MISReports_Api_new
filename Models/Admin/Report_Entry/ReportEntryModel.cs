using System;

namespace MISReports_Api.Models
{
    public class ReportEntryModel
    {
        public string RepId { get; set; }
        public string CatCode { get; set; }
        public string RepName { get; set; }
        public int Favorite { get; set; }
        public int Active { get; set; }
    }
}




