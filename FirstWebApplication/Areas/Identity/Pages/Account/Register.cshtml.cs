using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;

namespace FirstWebApplication.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IEnumerable<SelectListItem> Organisasjoner { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "{0} må være minst {2} tegn.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Passord")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Bekreft passord")]
            [Compare("Password", ErrorMessage = "Passordene er ikke like.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Fornavn")]
            public string Fornavn { get; set; }

            [Required]
            [Display(Name = "Etternavn")]
            public string Etternavn { get; set; }

            // Denne settes via JavaScript i Wizard-en
            [Required]
            public string SelectedRole { get; set; }

            // Kan være null hvis det er Registerfører (vi setter det automatisk)
            public long? OrganisasjonId { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            // Last inn organisasjoner, men ekskluder "Kartverket" fra listen pilotene ser
            Organisasjoner = _context.Organisasjoner
                .Where(o => o.Name != "Kartverket")
                .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // 1. Manuell validering av Organisasjon basert på rolle
            if (Input.SelectedRole == "Pilot" && Input.OrganisasjonId == null)
            {
                ModelState.AddModelError("Input.OrganisasjonId", "Piloter må velge en organisasjon.");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    Fornavn = Input.Fornavn,
                    Etternavn = Input.Etternavn,
                    IsApproved = false, // MÅ GODKJENNES AV ADMIN
                    RegisteredDate = DateTime.Now
                };

                // 2. Håndter Organisasjon basert på rolle
                if (Input.SelectedRole == "Registerfører")
                {
                    var kartverket = await _context.Organisasjoner.FirstOrDefaultAsync(o => o.Name == "Kartverket");
                    if (kartverket != null)
                    {
                        user.OrganisasjonId = kartverket.Id;
                    }
                }
                else
                {
                    user.OrganisasjonId = Input.OrganisasjonId;
                }

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account.");

                    // 3. Tildel rolle
                    if (Input.SelectedRole == "Registerfører")
                    {
                        await _userManager.AddToRoleAsync(user, "Registerfører");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Pilot");
                    }

                    // Sender brukeren til en infoside i stedet for å logge inn
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Hvis feil, last inn listen igjen
            Organisasjoner = _context.Organisasjoner
                .Where(o => o.Name != "Kartverket")
                .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
                .ToList();

            return Page();
        }
    }
}