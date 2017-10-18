using PortfolioSite.Utils;
using SandyUtils.Utils;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace PortfolioSite
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            RegisterApis();
            ConfigureSessionManager();
        }

        private static void RegisterApis()
        {
            var apiConnsFilePath = $"{ConfigurationManager.AppSettings["ApiConnectionsFileName"]}";
            var apiName = $"{ConfigurationManager.AppSettings["PortfolioApiName"]}";
            var apiConn = CredentialManager.GetApiConnection(apiName, apiConnsFilePath);
            var pfApiMediator = new PortfolioApiMediator(apiConn.Uri, apiConn.ApiKey);

            SandyUtils.Utils.DependencyResolver.SetService(pfApiMediator);

            // REGISTER GITHUB API HERE
        }

        private static void ConfigureSessionManager()
        {
            // Database connection
            var pfConnString = ConfigurationManager.ConnectionStrings["PortfolioTokenDBEntities"].ConnectionString;
            var credentialsFilePath = $"{ConfigurationManager.AppSettings["CredentialsFileName"]}";
            var credentials = CredentialManager.GetCredentials("PortfolioTokenDBEntities", credentialsFilePath);
            pfConnString = pfConnString.Replace("{userid}", credentials.UserId)
                                        .Replace("{password}", credentials.Password);

            SessionManager.Current.ContextConnectionString = pfConnString;
        }
    }
}
