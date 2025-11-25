using FirstWebApplication.Entities;
using FirstWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FirstWebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _signInManager = signInManager;
        }

        // Viser forsiden med innloggings-/registreringsskjema
        // Logger ut eksisterende brukere for clean slate ved hver demo
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // For Expo demo: Logg ut automatisk for å starte fresh hver gang
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                await _signInManager.SignOutAsync();
                // Redirect tilbake til Index for å vise login-skjema
                return RedirectToAction("Index");
            }

            // Vis registreringsform hvis registrering feilet
            if (TempData["ShowRegister"] != null)
            {
                ViewBag.ShowRegister = true;

                if (TempData["RegisterErrors"] != null)
                {
                    var errors = TempData["RegisterErrors"]?.ToString()?.Split('|');
                    if (errors != null)
                    {
                        foreach (var error in errors)
                        {
                            if (!string.IsNullOrEmpty(error))
                                ModelState.AddModelError(string.Empty, error);
                        }
                    }
                }
            }
            // Vis innloggingsfeil hvis innlogging feilet
            else if (TempData["LoginErrors"] != null)
            {
                var errors = TempData["LoginErrors"]?.ToString()?.Split('|');
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        if (!string.IsNullOrEmpty(error))
                            ModelState.AddModelError(string.Empty, error);
                    }
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
