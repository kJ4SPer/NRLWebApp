using System;
using FirstWebApplication.Models.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace FirstWebApplication.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index(string? returnUrl = null)
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();

            var model = new SettingsViewModel
            {
                CurrentLanguage = requestCulture?.RequestCulture.UICulture.Name ?? "nb-NO"
            };

            ViewData["ReturnUrl"] = returnUrl ?? HttpContext.Request.Path + HttpContext.Request.QueryString;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(SettingsViewModel model, string? returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(model.CurrentLanguage)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
