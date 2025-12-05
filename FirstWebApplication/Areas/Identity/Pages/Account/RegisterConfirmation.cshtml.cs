using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirstWebApplication.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public void OnGet(string email)
        {
            // Vi tar imot e-posten bare hvis vi vil vise den, 
            // men foreløpig holder det med en generell melding.
        }
    }
}