using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using FirstWebApplication.Entities; // VIKTIG: Sjekk at denne matcher din ApplicationUser

namespace FirstWebApplication.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            // Send brukeren tilbake til forsiden (Home/Index) etter utlogging
            return RedirectToPage("/Index");
            // Eller hvis du vil til MVC-kontrolleren: 
            // return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}