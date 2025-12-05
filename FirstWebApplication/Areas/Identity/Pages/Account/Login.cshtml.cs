using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using FirstWebApplication.Entities; // VIKTIG: Sjekk at denne peker på dine Entities

namespace FirstWebApplication.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; } 
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Tømmer eksisterende eksterne cookies for å sikre en ren login
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Forsøk å logge inn
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // Hent brukeren for å sjekke roller og godkjenning
                    var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);

                    // --- SJEKK OM BRUKER ER GODKJENT ---
                    if (!user.IsApproved)
                    {
                        _logger.LogWarning("User {Email} tried to login but is not approved.", Input.Email);

                        // Vi sender dem til "Pending"-siden. 
                        // Merk: De er teknisk sett innlogget nå (Identity cookie er satt), 
                        // men Pending-siden gir dem ingen muligheter annet enn å logge ut.
                        return RedirectToPage("AccountPending");
                    }
                    // -----------------------------------

                    var roles = await _signInManager.UserManager.GetRolesAsync(user);

                    if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("AdminDashboard", "Admin");
                    }
                    else if (roles.Contains("Registerfører"))
                    {
                        return RedirectToAction("RegisterforerDashboard", "Registerforer");
                    }
                    else
                    {
                        return RedirectToAction("MyRegistrations", "Pilot");
                    }
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // Hvis vi kom hit, feilet noe -> vis skjema på nytt
            return Page();
        }
    }
}