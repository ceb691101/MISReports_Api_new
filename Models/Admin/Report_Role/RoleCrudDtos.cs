using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class RoleCrudRequest
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string Password { get; set; }
        public string UserType { get; set; }   // User / Administrator
        public string Company { get; set; }
        public string CompSub { get; set; }    // maps to comp_dup
    }

    public class RoleCostCentreRequest
    {
        public string RoleId { get; set; }
        public string CostCentre { get; set; }
        public int LvlNo { get; set; }
    }

    public class DeleteByCategoryRequest
    {
        public string RoleId { get; set; }
        public List<string> CatCodes { get; set; }
    }
}