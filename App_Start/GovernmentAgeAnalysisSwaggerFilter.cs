using MISReports_Api.Controllers;
using Swashbuckle.Swagger;
using System.Collections.Generic;
using System.Web.Http.Description;

namespace MISReports_Api
{
    public class GovernmentAgeAnalysisSwaggerFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (apiDescription.ActionDescriptor.ControllerDescriptor.ControllerType != typeof(GovernmentAgeAnalysisController))
                return;

            if (apiDescription.ActionDescriptor.ActionName == "GetSectors")
            {
                operation.summary = "Government bulk report sectors and available bill cycles";
                operation.description = "Use data.sectors[].CatCode as sectorCode and a value from data.billCycles as billCycle when generating the report.";
                return;
            }

            bool locations = apiDescription.ActionDescriptor.ActionName == "GetLocations";
            operation.summary = locations ? "Province and division choices for government bulk age analysis"
                : "Age Analysis For Government Customers (Bulk Customers)";
            operation.description = locations
                ? "Select Province or Division to load location choices. Use the returned Code as locationCode. Entire CEB returns an empty list because no location selection is needed."
                : "Detailed returns accounts in the selected sector. Full returns accounts in all sectors. Summary returns totals by sector. Every report is restricted to the selected province or division, or covers Entire CEB. Report labels such as 'Full report' are also accepted.";
            foreach (var parameter in operation.parameters)
            {
                switch (parameter.name)
                {
                    case "billCycle":
                        parameter.required = true;
                        parameter.description = "Required. Use an available bill cycle from GET /api/debtors/bulk/sectors.";
                        break;
                    case "reportType":
                        parameter.@enum = new List<object> { "Detailed", "Full", "Summary" };
                        parameter.@default = "Detailed";
                        parameter.description = "Select the report type. Full and Summary include all sectors.";
                        break;
                    case "sectorCode":
                        parameter.@default = "A";
                        parameter.description = "Required for Detailed only. Use a CatCode from GET /api/debtors/bulk/sectors: for example A = Agriculture or H = Health. Codes are letters, not sector numbers. Ignored for Full and Summary.";
                        break;
                    case "areaType":
                        parameter.@enum = new List<object> { "Province", "Division", "Entire CEB" };
                        parameter.@default = "Entire CEB";
                        parameter.description = locations
                            ? "Select Province or Division to load the matching locations. Entire CEB needs no location selection."
                            : "Select Province, Division, or Entire CEB. The locationCode dropdown loads the matching provinces or divisions.";
                        break;
                    case "locationCode":
                        parameter.description = "Choose a province or division after selecting Area Type. The dropdown loads valid codes automatically. Disabled for Entire CEB.";
                        break;
                }
            }
            operation.responses["400"] = new Response { description = "Invalid report parameters. The response Message explains the required correction." };
        }
    }
}
