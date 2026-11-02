using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.ExceptionHandling;

namespace MISReports_Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var cors = new EnableCorsAttribute("*", "*", "*");
            // (origins, headers, methods)
            config.EnableCors(cors);

            // Logging: every API request/response, plus unhandled exceptions
            config.Filters.Add(new ApiRequestLoggingFilter());
            config.Services.Add(typeof(IExceptionLogger), new NLogExceptionLogger());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}