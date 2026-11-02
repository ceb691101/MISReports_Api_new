
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace MISReports_Api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            NLog.LogManager.GetCurrentClassLogger().Info("MISReports_Api application started.");
        }

        protected void Application_Error()
        {
            // Unhandled errors outside Web API (MVC pages, static content, startup)
            var ex = Server.GetLastError();
            if (ex != null)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Unhandled application error. Url={url}",
                    Context?.Request?.RawUrl);
            }
        }

        protected void Application_End()
        {
            // NLog targets are async; flush pending log entries before IIS
            // recycles or stops the app pool so the last messages are not lost.
            NLog.LogManager.GetCurrentClassLogger().Info("MISReports_Api application stopping. Reason={reason}",
                System.Web.Hosting.HostingEnvironment.ShutdownReason);
            NLog.LogManager.Shutdown();
        }
    }
}
