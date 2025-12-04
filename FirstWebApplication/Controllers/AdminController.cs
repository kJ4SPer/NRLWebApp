using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FirstWebApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // =========================================================
        // 1. DASHBOARD
        // =========================================================
        public async Task<IActionResult> AdminDashboard()
        {
            // Vi henter kun tallene vi trenger for de to boksene
            ViewBag.PendingCount = await _userManager.Users.CountAsync(u => !u.IsApproved);
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            return View();
        }

        // =========================================================
        // 2. OPPGAVE: GODKJENNE NYE BRUKERE (Pending)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> AdminPendingUsers()
        {
            var pendingUsers = await _userManager.Users
                .Where(u => u.IsApproved == false)
                .Include(u => u.Organisasjon)
                .OrderByDescending(u => u.RegisteredDate)
                .ToListAsync();

            return View(pendingUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"{user.Email} ble godkjent.";
            }
            return RedirectToAction(nameof(AdminPendingUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = "Søknaden ble avvist og slettet.";
            }
            return RedirectToAction(nameof(AdminPendingUsers));
        }

        // =========================================================
        // 3. OPPGAVE: ADMINISTRERE EKSISTERENDE BRUKERE
        // =========================================================

        // Liste over alle brukere
        public async Task<IActionResult> AdminUsers()
        {
            var users = await _userManager.Users
                .Include(u => u.Organisasjon)
                .OrderBy(u => u.Fornavn)
                .ToListAsync();

            var model = new List<UserViewModel>();
            foreach (var user in users)
            {
                model.Add(new UserViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Fornavn = user.Fornavn,
                    Etternavn = user.Etternavn,
                    OrganisasjonNavn = user.Organisasjon?.Name ?? "Ingen", // Hvis null, bruk "Ingen"
                    Roller = await _userManager.GetRolesAsync(user)
                });
            }
            return View(model);
        }

        // REDIGER BRUKER (GET)
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.Users.Include(u => u.Organisasjon).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Fornavn = user.Fornavn,
                Etternavn = user.Etternavn,
                OrganisasjonId = user.OrganisasjonId,
                CurrentRole = userRoles.FirstOrDefault() // Vi antar én hovedrolle for enkelhets skyld
            };

            // Fyll dropdowns
            ViewBag.Organisasjoner = new SelectList(_context.Organisasjoner, "Id", "Name", user.OrganisasjonId);
            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));

            return View(model);
        }

        // REDIGER BRUKER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Oppdater info
                user.Fornavn = model.Fornavn;
                user.Etternavn = model.Etternavn;
                user.Email = model.Email;
                user.UserName = model.Email; // Holder disse like
                user.OrganisasjonId = model.OrganisasjonId;

                await _userManager.UpdateAsync(user);

                // Oppdater rolle (Fjern alle gamle, legg til ny)
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!string.IsNullOrEmpty(model.CurrentRole))
                {
                    await _userManager.AddToRoleAsync(user, model.CurrentRole);
                }

                TempData["SuccessMessage"] = "Bruker oppdatert!";
                return RedirectToAction(nameof(AdminUsers));
            }

            // Reload dropdowns ved feil
            ViewBag.Organisasjoner = new SelectList(_context.Organisasjoner, "Id", "Name", model.OrganisasjonId);
            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));
            return View(model);
        }

        // SLETT BRUKER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = "Bruker slettet.";
            }
            return RedirectToAction(nameof(AdminUsers));
        }
    }
}