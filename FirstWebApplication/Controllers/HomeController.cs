using FirstWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FirstWebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Viser forsiden med innloggings-/registreringsskjema
        // Redirecter innloggede brukere til riktig dashboard basert på rolle
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Redirect innloggede brukere til riktig dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("AdminDashboard", "Admin");

                if (User.IsInRole("Registerfører"))
                    return RedirectToAction("RegisterforerDashboard", "Registerforer");

                return RedirectToAction("RegisterType", "Pilot");
            }

            // Vis registreringsform hvis registrering feilet
            if (TempData["ShowRegister"] != null)
            {
                ViewBag.ShowRegister = true;

                if (TempData["RegisterErrors"] != null)
                {
                    var errors = TempData["RegisterErrors"].ToString().Split('|');
                    foreach (var error in errors)
                    {
                        if (!string.IsNullOrEmpty(error))
                            ModelState.AddModelError(string.Empty, error);
                    }
                }
            }
            // Vis innloggingsfeil hvis innlogging feilet
            else if (TempData["LoginErrors"] != null)
            {
                var errors = TempData["LoginErrors"].ToString().Split('|');
                foreach (var error in errors)
                {
                    if (!string.IsNullOrEmpty(error))
                        ModelState.AddModelError(string.Empty, error);
                }
            }

            return View();
        }

        // Viser personvernside
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // Viser feilside med request ID for debugging
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
