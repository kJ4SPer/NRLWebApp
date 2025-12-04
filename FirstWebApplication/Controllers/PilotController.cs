using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle; // Sørg for at denne mappen eksisterer
using FirstWebApplication.Models.Enums;    // Enum for statuser
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FirstWebApplication.Controllers
{
    [Authorize(Roles = "Pilot")]
    public class PilotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PilotController> _logger;

        public PilotController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<PilotController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // =============================================================
        // 1. REGISTRERINGS-VALG
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> RegisterType()
        {
            if (!await IsUserApproved()) return RedirectToAction("AccountPending", "Account", new { area = "Identity" });
            return View();
        }

        // =============================================================
        // 2. HURTIG-REGISTRERING (KUN KART)
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> QuickRegister()
        {
            if (!await IsUserApproved()) return RedirectToAction("AccountPending", "Account", new { area = "Identity" });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickRegister(string obstacleGeometry)
        {
            if (!await IsUserApproved()) return RedirectToAction("AccountPending", "Account", new { area = "Identity" });

            if (string.IsNullOrEmpty(obstacleGeometry))
            {
                TempData["ErrorMessage"] = "Du må markere hinderets posisjon på kartet.";
                return View();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // TRANSAKSJON FOR SIKKER LAGRING
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Opprett hinder
                var obstacle = new Obstacle
                {
                    Location = obstacleGeometry,
                    RegisteredByUserId = userId,
                    RegisteredDate = DateTime.Now,
                    Name = "" // Tomt navn markerer den som uferdig
                };

                _context.Obstacles.Add(obstacle);
                await _context.SaveChangesAsync();

                // 2. Opprett status "Registered" 
                var status = CreateObstacleStatus(obstacle.Id, (int)ObstacleStatusEnum.Registered, userId, "Hurtigregistrering opprettet");
                _context.ObstacleStatuses.Add(status);
                await _context.SaveChangesAsync();

                // 3. Koble status tilbake til hinder
                obstacle.CurrentStatusId = status.Id;
                _context.Obstacles.Update(obstacle);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Posisjon lagret! Du kan fullføre registreringen senere.";
                return RedirectToAction("RegisterType");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Feil under QuickRegister");
                TempData["ErrorMessage"] = "Noe gikk galt under lagring.";
                return View();
            }
        }

        // =============================================================
        // 3. FULL REGISTRERING (FIXED)
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> FullRegister()
        {
            // Sjekk om bruker er godkjent før de får se skjemaet
            if (!await IsUserApproved())
                return RedirectToAction("AccountPending", "Account", new { area = "Identity" });

            // Returner blankt skjema
            return View(new RegisterObstacleViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FullRegister(RegisterObstacleViewModel model, string? CustomObstacleType)
        {
            if (!await IsUserApproved()) return RedirectToAction("AccountPending", "Account", new { area = "Identity" });

            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // 1. Lag Execution Strategy
            var strategy = _context.Database.CreateExecutionStrategy();

            // 2. Kjør transaksjonen (VIKTIG ENDRING: <IActionResult> er lagt til her!)
            // Dette forteller kompilatoren at vi forventer et resultat tilbake.
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // A. Opprett hinder
                    var obstacle = new Obstacle
                    {
                        Name = model.ObstacleName,
                        Height = model.ObstacleHeight,
                        Description = model.ObstacleDescription,
                        Location = model.ObstacleGeometry,
                        RegisteredByUserId = userId,
                        RegisteredDate = DateTime.Now
                    };

                    await SetObstacleTypeAsync(obstacle, model.ObstacleType, CustomObstacleType);

                    _context.Obstacles.Add(obstacle);
                    await _context.SaveChangesAsync();

                    // B. Opprett status "Pending" 
                    var status = CreateObstacleStatus(obstacle.Id, (int)Models.Enums.ObstacleStatusEnum.Pending, userId, "Full registrering sendt inn");
                    _context.ObstacleStatuses.Add(status);
                    await _context.SaveChangesAsync();

                    // C. Koble sammen
                    obstacle.CurrentStatusId = status.Id;
                    _context.Obstacles.Update(obstacle);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Hinder registrert! Venter nå på godkjenning.";
                    return RedirectToAction("MyRegistrations");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Feil under FullRegister");
                    ModelState.AddModelError("", "Kunne ikke lagre hinderet. Prøv igjen.");

                    // Vi må returnere Viewet her inne også
                    return View(model);
                }
            });
        }

        // =============================================================
        // 4. FULLFØR HURTIGREGISTRERING
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> CompleteQuickRegister(long id)
        {
            var userId = _userManager.GetUserId(User);

            var obstacle = await _context.Obstacles
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == id && o.RegisteredByUserId == userId);

            // Sjekk at den er "Registered"
            if (obstacle == null || obstacle.CurrentStatus?.StatusTypeId != (int)ObstacleStatusEnum.Registered)
            {
                TempData["ErrorMessage"] = "Fant ikke uferdig registrering.";
                return RedirectToAction("MyRegistrations");
            }

            var model = new CompleteQuickRegViewModel
            {
                ObstacleId = obstacle.Id,
                ObstacleGeometry = obstacle.Location,
                RegisteredDate = obstacle.RegisteredDate,
                RegisteredBy = obstacle.RegisteredByUser?.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteQuickRegister(CompleteQuickRegViewModel model, string? CustomObstacleType)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var obstacle = await _context.Obstacles
                    .Include(o => o.CurrentStatus)
                    .FirstOrDefaultAsync(o => o.Id == model.ObstacleId && o.RegisteredByUserId == userId);

                if (obstacle == null) return NotFound();

                // 1. Oppdater hinder-data
                obstacle.Name = model.ObstacleName;
                obstacle.Height = model.ObstacleHeight;
                obstacle.Description = model.ObstacleDescription;
                await SetObstacleTypeAsync(obstacle, model.ObstacleType, CustomObstacleType);

                // 2. Deaktiver gammel status
                if (obstacle.CurrentStatus != null)
                {
                    obstacle.CurrentStatus.IsActive = false;
                    _context.ObstacleStatuses.Update(obstacle.CurrentStatus);
                }

                // 3. Legg til ny status (Pending)
                var newStatus = CreateObstacleStatus(obstacle.Id, (int)ObstacleStatusEnum.Pending, userId, "Hurtigregistrering fullført");
                _context.ObstacleStatuses.Add(newStatus);
                await _context.SaveChangesAsync();

                // 4. Oppdater peker
                obstacle.CurrentStatusId = newStatus.Id;
                _context.Obstacles.Update(obstacle);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Registrering fullført!";
                return RedirectToAction("MyRegistrations");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Feil under CompleteQuickRegister");
                return View(model);
            }
        }

        // =============================================================
        // 5. MINE REGISTRERINGER OG OVERSIKT
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            var userId = _userManager.GetUserId(User);

            var obstacles = await _context.Obstacles
                .AsNoTracking()
                .Include(o => o.CurrentStatus).ThenInclude(s => s!.StatusType) // FIKSET: Null-safe include
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Include(o => o.StatusHistory).ThenInclude(sh => sh.ChangedByUser)
                .Where(o => o.RegisteredByUserId == userId)
                .OrderByDescending(o => o.RegisteredDate)
                .ToListAsync();

            var viewModel = BuildMyRegistrationsViewModel(obstacles);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Overview(long id)
        {
            var userId = _userManager.GetUserId(User);

            var obstacle = await _context.Obstacles
                .AsNoTracking()
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus).ThenInclude(s => s!.StatusType)
                .Include(o => o.StatusHistory).ThenInclude(sh => sh.StatusType)
                .Include(o => o.StatusHistory).ThenInclude(sh => sh.ChangedByUser)
                .FirstOrDefaultAsync(o => o.Id == id && o.RegisteredByUserId == userId);

            if (obstacle == null) return NotFound();

            var viewModel = BuildObstacleDetailsViewModel(obstacle);
            return View(viewModel);
        }

        // =============================================================
        // 6. SLETT REGISTRERING (MED EXECUTION STRATEGY)
        // =============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegistration(long id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // 1. Lag en strategi for å håndtere "retry" ved databasefeil
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                // 2. Kjør alt innenfor strategien
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Hent hinderet (Må hentes på nytt innenfor strategien)
                        var obstacle = await _context.Obstacles
                            .FirstOrDefaultAsync(o => o.Id == id && o.RegisteredByUserId == userId);

                        if (obstacle == null)
                        {
                            // Vi kan ikke returnere Redirect herfra direkte i en lambda, 
                            // så vi kaster en exception for å hoppe ut, eller håndterer det etterpå.
                            // For enkelhets skyld sjekker vi null her og lar koden fortsette hvis den finner den.
                            return;
                        }

                        // STEG 1: BRYT SIRKELEN
                        obstacle.CurrentStatusId = null;
                        _context.Obstacles.Update(obstacle);
                        await _context.SaveChangesAsync();

                        // STEG 2: SLETT STATUS-HISTORIKK
                        var statuses = await _context.ObstacleStatuses
                            .Where(s => s.ObstacleId == id)
                            .ToListAsync();

                        _context.ObstacleStatuses.RemoveRange(statuses);
                        await _context.SaveChangesAsync();

                        // STEG 3: SLETT HINDERET
                        _context.Obstacles.Remove(obstacle);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        TempData["SuccessMessage"] = "Registreringen ble slettet.";
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw; // Kast videre for å logge i ytre blokk
                    }
                });

                // Sjekk om meldingen ble satt (betyr at vi fant og slettet hinderet)
                if (TempData["SuccessMessage"] == null)
                {
                    TempData["ErrorMessage"] = "Fant ikke registreringen, eller noe gikk galt.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feil under sletting av obstacle {Id}", id);
                TempData["ErrorMessage"] = "En feil oppstod under sletting.";
            }

            return RedirectToAction("MyRegistrations");
        }

        // =============================================================
        // API-METODER FOR JAVASCRIPT (Quick Register)
        // =============================================================


        // 2. API-versjon av QuickRegister (Returnerer JSON i stedet for View)
        [HttpPost]
        [Route("Pilot/QuickRegisterApi")]
        // [ValidateAntiForgeryToken]  <-- Denne er fjernet/kommentert ut
        public async Task<IActionResult> QuickRegisterApi(string obstacleGeometry)
        {
            if (string.IsNullOrEmpty(obstacleGeometry))
            {
                return Json(new { success = false, message = "Mangler posisjon." });
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                // 1. Lagre hinderet først (uten status)
                var obstacle = new Obstacle
                {
                    Location = obstacleGeometry,
                    RegisteredByUserId = userId,
                    RegisteredDate = DateTime.Now,
                    Name = "" // Tomt navn markerer den som uferdig
                };

                _context.Obstacles.Add(obstacle);
                await _context.SaveChangesAsync(); // Her får obstacle en ID

                // 2. Opprett status
                var status = new ObstacleStatus
                {
                    ObstacleId = obstacle.Id,
                    StatusTypeId = (int)Models.Enums.ObstacleStatusEnum.Registered,
                    ChangedByUserId = userId,
                    ChangedDate = DateTime.Now,
                    Comments = "Hurtigregistrering (API)",
                    IsActive = true
                };

                _context.ObstacleStatuses.Add(status);
                await _context.SaveChangesAsync(); // VIKTIG! Lagre statusen for å få en ID

                // 3. Koble status ID tilbake til hinderet
                obstacle.CurrentStatusId = status.Id; // Nå har status.Id en ekte verdi (ikke 0)
                _context.Obstacles.Update(obstacle);

                await _context.SaveChangesAsync(); // Lagre koblingen

                return Json(new { success = true, id = obstacle.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feil i QuickRegisterApi");
                return Json(new { success = false, message = "Lagring feilet på serveren." });
            }
        }

        // =============================================================
        // HJELPEMETODER
        // =============================================================

        private async Task<bool> IsUserApproved()
        {
            var user = await _userManager.GetUserAsync(User);
            return user != null && user.IsApproved;
        }

        private ObstacleStatus CreateObstacleStatus(long obstacleId, int statusTypeId, string userId, string comments)
        {
            return new ObstacleStatus
            {
                ObstacleId = obstacleId,
                StatusTypeId = statusTypeId,
                ChangedByUserId = userId,
                ChangedDate = DateTime.Now,
                Comments = comments,
                IsActive = true
            };
        }

        private async Task SetObstacleTypeAsync(Obstacle obstacle, string? obstacleType, string? customType)
        {
            if (obstacleType == "Other" && !string.IsNullOrWhiteSpace(customType))
            {
                var existingType = await _context.ObstacleTypes
                    .FirstOrDefaultAsync(ot => ot.Name.ToLower() == customType.ToLower());

                if (existingType != null)
                {
                    obstacle.ObstacleTypeId = existingType.Id;
                }
                else
                {
                    var newType = new ObstacleType
                    {
                        Name = customType,
                        Description = "Egendefinert type",
                        MinHeight = 0,
                        MaxHeight = 9999
                    };
                    _context.ObstacleTypes.Add(newType);
                    await _context.SaveChangesAsync();
                    obstacle.ObstacleTypeId = newType.Id;
                }
            }
            else if (!string.IsNullOrWhiteSpace(obstacleType))
            {
                var type = await _context.ObstacleTypes.FirstOrDefaultAsync(ot => ot.Name == obstacleType);
                if (type != null) obstacle.ObstacleTypeId = type.Id;
            }
        }

        private MyRegistrationsViewModel BuildMyRegistrationsViewModel(List<Obstacle> obstacles)
        {
            var viewModel = new MyRegistrationsViewModel();

            foreach (var obstacle in obstacles)
            {
                var statusId = obstacle.CurrentStatus?.StatusTypeId ?? 0;

                if (statusId == (int)ObstacleStatusEnum.Registered)
                {
                    viewModel.IncompleteQuickRegs.Add(new IncompleteQuickRegItem
                    {
                        Id = obstacle.Id,
                        Location = obstacle.Location,
                        RegisteredDate = obstacle.RegisteredDate
                    });
                }
                else if (statusId == (int)ObstacleStatusEnum.Pending) viewModel.PendingObstacles.Add(MapToListItem(obstacle));
                else if (statusId == (int)ObstacleStatusEnum.Approved) viewModel.ApprovedObstacles.Add(MapToListItem(obstacle));
                else if (statusId == (int)ObstacleStatusEnum.Rejected) viewModel.RejectedObstacles.Add(MapToListItem(obstacle));
            }
            return viewModel;
        }

        private ObstacleListItemViewModel MapToListItem(Obstacle obstacle)
        {
            var statusName = obstacle.CurrentStatus?.StatusType?.Name ?? "Ukjent";

            var lastProcessor = obstacle.StatusHistory?
                .OrderByDescending(sh => sh.ChangedDate)
                .FirstOrDefault(sh => sh.ChangedByUserId != obstacle.RegisteredByUserId)?.ChangedByUser?.Email;

            return new ObstacleListItemViewModel
            {
                Id = obstacle.Id,
                Name = obstacle.Name ?? "Uten navn",
                Height = obstacle.Height ?? 0,
                Type = obstacle.ObstacleType?.Name ?? "Ukjent",
                Location = obstacle.Location,
                RegisteredDate = obstacle.RegisteredDate,
                CurrentStatus = statusName,
                ProcessedBy = lastProcessor,
                RejectionReason = (obstacle.CurrentStatus?.StatusTypeId == (int)ObstacleStatusEnum.Rejected) ? obstacle.CurrentStatus.Comments : null
            };
        }

        private ObstacleDetailsViewModel BuildObstacleDetailsViewModel(Obstacle obstacle)
        {
            return new ObstacleDetailsViewModel
            {
                Id = obstacle.Id,
                Name = obstacle.Name,
                Height = obstacle.Height ?? 0,
                Description = obstacle.Description,
                Type = obstacle.ObstacleType?.Name,
                Location = obstacle.Location,
                RegisteredDate = obstacle.RegisteredDate,
                CurrentStatus = obstacle.CurrentStatus?.StatusType?.Name,
                StatusHistory = obstacle.StatusHistory?.OrderByDescending(sh => sh.ChangedDate).Select(sh => new StatusHistoryItem
                {
                    Status = sh.StatusType?.Name,
                    ChangedBy = sh.ChangedByUser?.Email,
                    ChangedDate = sh.ChangedDate,
                    Comments = sh.Comments
                }).ToList() ?? new List<StatusHistoryItem>()
            };
        }
    }
}