// Models/Admin/RepRoles/CreateRoleRequest.cs
namespace MISReports_Api.Models
{
    public class CreateRoleRequest
    {
        public string EpfNo { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string UserType { get; set; }
        public string Company { get; set; }
        public string UserGroup { get; set; }
        public string CostCentre { get; set; }
        public int LvlNo { get; set; }
    }
}