using System;
using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class CostCenterModel
    {
        public string CostCenterId { get; set; }
        public string CostCenterName { get; set; }
        public string CostCenterDisplay { get; set; } // Formatted as "ID:Name"
        public int LevelNo { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CompanyModel
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDisplay { get; set; } // Formatted as "ID : Name"
        public string ParentId { get; set; }
        public string GroupCompany { get; set; }
    }

    public class CostCenterLoadRequest
    {
        public string CompanyId { get; set; }
        public string RoleId { get; set; }
    }

    public class CostCenterSaveRequest
    {
        public string RoleId { get; set; }
        public string CompanyId { get; set; }
        public List<string> CostCenterIds { get; set; }
    }

    public class CostCenterLoadResponse
    {
        public CompanyModel Company { get; set; }
        public List<CostCenterModel> AvailableCostCenters { get; set; }
        public List<string> SelectedCostCenters { get; set; }
        public string ErrorMessage { get; set; }
    }
}
