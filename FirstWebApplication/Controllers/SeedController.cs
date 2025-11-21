using FirstWebApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstWebApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SeedController : Controller
    {
        private readonly DatabaseSeeder _seeder;

        public SeedController(DatabaseSeeder seeder)
        {
            _seeder = seeder;
        }

        // Viser siden hvor admin kan kjøre database seeding
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Kjører database seeding for å generere testdata
        // Oppretter piloter, hindringtyper og hindringer
        [HttpPost]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                await _seeder.SeedAllDataAsync();
                TempData["Success"] = "Database seeding fullfort! 20 piloter, 8 obstacle types, og 150 hindringer ble opprettet.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Feil under seeding: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
