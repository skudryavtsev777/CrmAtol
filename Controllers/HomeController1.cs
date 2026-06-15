using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtol.Controllers
{
    public class HomeController1 : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "Reports");
            }
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}