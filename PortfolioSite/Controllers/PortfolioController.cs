using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace PortfolioSite.Controllers
{
    [RoutePrefix("Portfolio")]
    public class PortfolioController : AuthController
    {
        // Get portfolio page based on username in url
        [HttpGet]
        [Route("{userName}")]
        [AllowAnonymous]
        public async Task<ActionResult> Index(string userName)
        {
            return View();
        }
    }
}