using PortfolioSite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace PortfolioSite.Controllers
{
    [RoutePrefix("Portal")]
    public class PortalController : AuthController
    {
        // Gets index
        // Redirect to admin page if logged in
        // Redirect to login page if not logged in
        [HttpGet]
        [Route("Index")]
        [Route("")]
        [AllowAnonymous]
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        // Gets login page
        [HttpGet]
        [Route("Login")]
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("Login")]
        [ValidateAntiForgeryToken]
        // Completes login action, return success indicator
        public async Task<ActionResult> Login(LoginModel model)
        {
            return View();
        }

        // Gets admin page, requires user to be logged in
        // Redirect to login page if not logged in
        [HttpGet]
        public async Task<ActionResult> Admin()
        {
            return View();
        }

        // Temporary admin result action, separate into multiple actions for implementation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async  Task<ActionResult> Admin(AdminModel model)
        {
            return View();
        }
    }
}