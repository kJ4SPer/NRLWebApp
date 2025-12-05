using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using FirstWebApplication.Models.Enums;
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

        [HttpGet]
        public async Task<IActionResult> RegisterType()
        {
            if (!await IsUserApproved()) return RedirectToAction("AccountPending", "Account", new { area = "Identity" });
            return View();
        }

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

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var obstacle = new Obstacle
                {
                    Location = obstacleGeometry,
                    RegisteredByUserId = userId,
                    RegisteredDate = DateTime.Now
                    // NAME ER FJERNET HERFRA
                };

                _context.Obstacles.Add(obstacle);
                await _context.SaveChangesAsync();

                var status = CreateObstacleStatus(obstacle.Id, (int)ObstacleStatusEnum.Registered, userId, "Hurtigregistrering opprettet");
                _context.ObstacleStatuses.Add(status);
                await _context.SaveChangesAsync();

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
        // 3. FULL REGISTRERING
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> FullRegister()
        {
            if (!await IsUserApproved())
                return RedirectToAction("AccountPending", "Account", new { area = "Identity" });

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

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Determine Type Name logic beholdes for typevalg, men brukes ikke til navn
                    string typeName = model.ObstacleType ?? "Hinder";
                    if (model.ObstacleType == "Other" && !string.IsNullOrWhiteSpace(CustomObstacleType))
                    {
                        typeName = CustomObstacleType;
                    }

                    // GENERERING AV NAVN ER FJERNET HER

                    var obstacle = new Obstacle
                    {
                        // NAME ER FJERNET HERFRA
                        Height = model.ObstacleHeight,
                        Description = model.ObstacleDescription,
                        Location = model.ObstacleGeometry,
                        RegisteredByUserId = userId,
                        RegisteredDate = DateTime.Now
                    };

                    await SetObstacleTypeAsync(obstacle, model.ObstacleType, CustomObstacleType);

                    _context.Obstacles.Add(obstacle);
                    await _context.SaveChangesAsync();

                    var status = CreateObstacleStatus(obstacle.Id, (int)Models.Enums.ObstacleStatusEnum.Pending, userId, "Full registrering sendt inn");
                    _context.ObstacleStatuses.Add(status);
                    await _context.SaveChangesAsync();

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
                    return View(model);
                }
            });
        }

        // =============================================================
        // 4. COMPLETE QUICK REGISTRATION
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> CompleteQuickRegister(long id)
        {
            var userId = _userManager.GetUserId(User);

            var obstacle = await _context.Obstacles
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == id && o.RegisteredByUserId == userId);

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

                // AUTOMATISK NAVN ER FJERNET HERFRA

                obstacle.Height = model.ObstacleHeight;
                obstacle.Description = model.ObstacleDescription;
                await SetObstacleTypeAsync(obstacle, model.ObstacleType, CustomObstacleType);

                if (obstacle.CurrentStatus != null)
                {
                    obstacle.CurrentStatus.IsActive = false;
                    _context.ObstacleStatuses.Update(obstacle.CurrentStatus);
                }

                var newStatus = CreateObstacleStatus(obstacle.Id, (int)ObstacleStatusEnum.Pending, userId, "Hurtigregistrering fullført");
                _context.ObstacleStatuses.Add(newStatus);
                await _context.SaveChangesAsync();

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

        // ... [Rest of controller remains unchanged] ...

        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            var userId = _userManager.GetUserId(User);
            var obstacles = await _context.Obstacles
                .AsNoTracking()
                .Include(o => o.CurrentStatus).ThenInclude(s => s!.StatusType)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegistration(long id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();
            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var obstacle = await _context.Obstacles
                            .FirstOrDefaultAsync(o => o.Id == id && o.RegisteredByUserId == userId);
                        if (obstacle == null) return;
                        obstacle.CurrentStatusId = null;
                        _context.Obstacles.Update(obstacle);
                        await _context.SaveChangesAsync();
                        var statuses = await _context.ObstacleStatuses.Where(s => s.ObstacleId == id).ToListAsync();
                        _context.ObstacleStatuses.RemoveRange(statuses);
                        await _context.SaveChangesAsync();
                        _context.Obstacles.Remove(obstacle);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        TempData["SuccessMessage"] = "Registreringen ble slettet.";
                    }
                    catch (Exception) { await transaction.RollbackAsync(); throw; }
                });
                if (TempData["SuccessMessage"] == null) TempData["ErrorMessage"] = "Fant ikke registreringen.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feil under sletting {Id}", id);
                TempData["ErrorMessage"] = "En feil oppstod under sletting.";
            }
            return RedirectToAction("MyRegistrations");
        }

        [HttpPost]
        [Route("Pilot/QuickRegisterApi")]
        public async Task<IActionResult> QuickRegisterApi(string obstacleGeometry)
        {
            if (string.IsNullOrEmpty(obstacleGeometry)) return Json(new { success = false, message = "Mangler posisjon." });
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();
            try
            {
                var obstacle = new Obstacle
                {
                    Location = obstacleGeometry,
                    RegisteredByUserId = userId,
                    RegisteredDate = DateTime.Now
                    // NAME ER FJERNET HERFRA
                };
                _context.Obstacles.Add(obstacle);
                await _context.SaveChangesAsync();
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
                await _context.SaveChangesAsync();
                obstacle.CurrentStatusId = status.Id;
                _context.Obstacles.Update(obstacle);
                await _context.SaveChangesAsync();
                return Json(new { success = true, id = obstacle.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feil i QuickRegisterApi");
                return Json(new { success = false, message = "Lagring feilet på serveren." });
            }
        }

        private async Task<bool> IsUserApproved()
        {
            var user = await _userManager.GetUserAsync(User);
            return user != null && user.IsApproved;
        }

        private ObstacleStatus CreateObstacleStatus(long obstacleId, int statusTypeId, string userId, string comments)
        {
            return new ObstacleStatus { ObstacleId = obstacleId, StatusTypeId = statusTypeId, ChangedByUserId = userId, ChangedDate = DateTime.Now, Comments = comments, IsActive = true };
        }

        private async Task SetObstacleTypeAsync(Obstacle obstacle, string? obstacleType, string? customType)
        {
            if (obstacleType == "Other" && !string.IsNullOrWhiteSpace(customType))
            {
                var existingType = await _context.ObstacleTypes.FirstOrDefaultAsync(ot => ot.Name.ToLower() == customType.ToLower());
                if (existingType != null) { obstacle.ObstacleTypeId = existingType.Id; }
                else
                {
                    var newType = new ObstacleType { Name = customType, Description = "Egendefinert type", MinHeight = 0, MaxHeight = 9999 };
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
                    viewModel.IncompleteQuickRegs.Add(new IncompleteQuickRegItem { Id = obstacle.Id, Location = obstacle.Location, RegisteredDate = obstacle.RegisteredDate });
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
            var lastProcessor = obstacle.StatusHistory?.OrderByDescending(sh => sh.ChangedDate).FirstOrDefault(sh => sh.ChangedByUserId != obstacle.RegisteredByUserId)?.ChangedByUser?.Email;
            return new ObstacleListItemViewModel
            {
                Id = obstacle.Id,
                // NAME ER FJERNET HER
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

                // NAME ER FJERNET HER

                Height = obstacle.Height ?? 0,
                Description = obstacle.Description ?? string.Empty,
                Type = obstacle.ObstacleType?.Name,
                Location = obstacle.Location ?? string.Empty,
                RegisteredDate = obstacle.RegisteredDate,
                CurrentStatus = obstacle.CurrentStatus?.StatusType?.Name ?? "Ukjent",
                StatusHistory = obstacle.StatusHistory?.OrderByDescending(sh => sh.ChangedDate).Select(sh => new StatusHistoryItem
                {
                    Status = sh.StatusType?.Name ?? "Ukjent",
                    ChangedBy = sh.ChangedByUser?.Email ?? "Ukjent bruker",
                    ChangedDate = sh.ChangedDate,
                    Comments = sh.Comments
                }).ToList() ?? new List<StatusHistoryItem>()
            };
        }
    }
}