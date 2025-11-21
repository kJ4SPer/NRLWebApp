using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FirstWebApplication.Controllers
{
    [Authorize(Roles = "Pilot")]
    public class PilotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PilotController> _logger;

        public PilotController(ApplicationDbContext context, ILogger<PilotController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Viser valgside for registreringstype (Quick eller Full)
        [HttpGet]
        public IActionResult RegisterType()
        {
            return View();
        }

        // Viser side for hurtigregistrering med kun kartmarkering
        [HttpGet]
        public IActionResult QuickRegister()
        {
            return View();
        }

        // Lagrer hurtigregistrering med kun posisjon
        // Oppretter ufullstendig obstacle som må kompletteres senere
        [HttpPost]
        public async Task<IActionResult> QuickRegister(string obstacleGeometry)
        {
            if (string.IsNullOrEmpty(obstacleGeometry))
            {
                TempData["ErrorMessage"] = "Please mark the obstacle location on the map";
                return View();
            }

            var userId = GetCurrentUserId();

            // Opprett obstacle med kun posisjon
            var obstacle = new Obstacle
            {
                Location = obstacleGeometry,
                RegisteredByUserId = userId,
                RegisteredDate = DateTime.Now
            };

            _context.Obstacles.Add(obstacle);
            await _context.SaveChangesAsync();

            // Opprett status "Registered" (ufullstendig)
            var status = CreateObstacleStatus(obstacle.Id, 1, userId, "Quick Register - location saved");
            _context.ObstacleStatuses.Add(status);
            await _context.SaveChangesAsync();

            // Koble status til obstacle
            obstacle.CurrentStatusId = status.Id;
            _context.Obstacles.Update(obstacle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Location saved! Please complete the registration.";
            return RedirectToAction("RegisterType");
        }

        // Viser fullstendig registreringsskjema
        [HttpGet]
        public IActionResult FullRegister()
        {
            return View(new RegisterObstacleViewModel());
        }

        // Behandler fullstendig registrering av hindring
        // Validerer og lagrer komplett hindring med Pending-status
        [HttpPost]
        public async Task<IActionResult> FullRegister(RegisterObstacleViewModel model, string? CustomObstacleType)
        {
            ValidateObstacleRegistration(model);

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            var obstacle = new Obstacle
            {
                Name = model.ObstacleName,
                Height = model.ObstacleHeight,
                Description = model.ObstacleDescription,
                Location = model.ObstacleGeometry,
                RegisteredByUserId = userId,
                RegisteredDate = DateTime.Now
            };

            // Sett obstacle type hvis oppgitt
            await SetObstacleTypeAsync(obstacle, model.ObstacleType, CustomObstacleType);

            _context.Obstacles.Add(obstacle);
            await _context.SaveChangesAsync();

            // Opprett status "Pending" (venter på godkjenning)
            var status = CreateObstacleStatus(obstacle.Id, 2, userId, "Full registration completed");
            _context.ObstacleStatuses.Add(status);
            await _context.SaveChangesAsync();

            obstacle.CurrentStatusId = status.Id;
            _context.Obstacles.Update(obstacle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Obstacle registered successfully!";
            return RedirectToAction("Overview", new { id = obstacle.Id });
        }

        // Viser skjema for å fullføre en hurtigregistrering
        [HttpGet]
        public async Task<IActionResult> CompleteQuickRegister(long id)
        {
            var userId = GetCurrentUserId();

            var obstacle = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s!.StatusType)
                .Include(o => o.RegisteredByUser)
                .Where(o => o.Id == id
                    && o.RegisteredByUserId == userId
                    && o.CurrentStatus!.StatusTypeId == 1)
                .FirstOrDefaultAsync();

            if (obstacle == null)
            {
                TempData["ErrorMessage"] = "Quick registration not found or already completed.";
                return RedirectToAction("MyRegistrations");
            }

            var viewModel = new CompleteQuickRegViewModel
            {
                ObstacleId = obstacle.Id,
                ObstacleGeometry = obstacle.Location,
                RegisteredDate = obstacle.RegisteredDate,
                RegisteredBy = obstacle.RegisteredByUser?.Email
            };

            return View(viewModel);
        }

        // Fullfører en hurtigregistrering med manglende detaljer
        // Oppdaterer status fra Registered til Pending
        [HttpPost]
        public async Task<IActionResult> CompleteQuickRegister(CompleteQuickRegViewModel model, string? CustomObstacleType)
        {
            ValidateQuickRegCompletion(model);

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            var obstacle = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                .Where(o => o.Id == model.ObstacleId
                    && o.RegisteredByUserId == userId
                    && o.CurrentStatus!.StatusTypeId == 1)
                .FirstOrDefaultAsync();

            if (obstacle == null)
            {
                TempData["ErrorMessage"] = "Quick registration not found or already completed.";
                return RedirectToAction("MyRegistrations");
            }

            // Oppdater obstacle med detaljer
            obstacle.Name = model.ObstacleName;
            obstacle.Height = model.ObstacleHeight;
            obstacle.Description = model.ObstacleDescription;

            await SetObstacleTypeAsync(obstacle, model.ObstacleType, CustomObstacleType);

            _context.Obstacles.Update(obstacle);

            // Deaktiver gammel status og opprett ny "Pending" status
            await UpdateObstacleStatusAsync(obstacle, 2, userId, "Quick registration completed");

            TempData["SuccessMessage"] = "Registration completed successfully!";
            return RedirectToAction("MyRegistrations");
        }

        // Viser oversikt over brukerens registreringer
        // Grupperer hindringer i: Ufullstendige, Pending, Approved, Rejected
        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            var userId = GetCurrentUserId();

            var obstacles = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s!.StatusType)
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Where(o => o.RegisteredByUserId == userId)
                .OrderByDescending(o => o.RegisteredDate)
                .ToListAsync();

            var obstacleIds = obstacles.Select(o => o.Id).ToList();

            var viewModel = BuildMyRegistrationsViewModel(obstacles);

            return View(viewModel);
        }

        // Viser detaljert oversikt over en hindring
        // Inkluderer statushistorikk og behandlingsinformasjon
        [HttpGet]
        public async Task<IActionResult> Overview(long id)
        {
            var userId = GetCurrentUserId();

            var obstacle = await _context.Obstacles
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s!.StatusType)
                .Include(o => o.StatusHistory)
                    .ThenInclude(sh => sh.StatusType)
                .Include(o => o.StatusHistory)
                    .ThenInclude(sh => sh.ChangedByUser)
                .Where(o => o.Id == id && o.RegisteredByUserId == userId)
                .FirstOrDefaultAsync();

            if (obstacle == null)
                return NotFound();

            var viewModel = BuildObstacleDetailsViewModel(obstacle);

            return View(viewModel);
        }

        // Sletter en hindring som er Registered eller Pending
        // Fjerner også alle relaterte statuser og behandlinger
        [HttpPost]
        public async Task<IActionResult> DeleteRegistration(long id)
        {
            var userId = GetCurrentUserId();

            var obstacle = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s!.StatusType)
                .Where(o => o.Id == id && o.RegisteredByUserId == userId)
                .FirstOrDefaultAsync();

            if (obstacle == null)
            {
                TempData["ErrorMessage"] = "Obstacle not found.";
                return RedirectToAction("MyRegistrations");
            }

            var statusName = obstacle.CurrentStatus?.StatusType?.Name;

            // Kan kun slette "Registered" eller "Pending"
            if (statusName != "Registered" && statusName != "Pending")
            {
                TempData["ErrorMessage"] = "You can only delete obstacles that are pending or incomplete.";
                return RedirectToAction("MyRegistrations");
            }

            await DeleteObstacleAndRelatedDataAsync(obstacle);

            TempData["SuccessMessage"] = "Obstacle deleted successfully.";
            return RedirectToAction("MyRegistrations");
        }

        // Viser pilot dashboard med statistikk over brukerens hindringer
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var statistics = await GetPilotStatisticsAsync(userId);

            ViewBag.IncompleteCount = statistics.Incomplete;
            ViewBag.PendingCount = statistics.Pending;
            ViewBag.ApprovedCount = statistics.Approved;
            ViewBag.RejectedCount = statistics.Rejected;
            ViewBag.TotalCount = statistics.Total;

            return View();
        }

        // Sjekker etter duplikathindringer innenfor gitt radius
        // Brukes for å varsle pilot om nærliggende hindringer
        [HttpGet]
        public async Task<IActionResult> CheckDuplicates(double latitude, double longitude, double radiusMeters = 10)
        {
            try
            {
                // Hent kun fullstendig registrerte obstacles
                var obstacles = await _context.Obstacles
                    .Include(o => o.ObstacleType)
                    .Include(o => o.CurrentStatus)
                    .Where(o => o.CurrentStatusId != null
                        && o.CurrentStatus != null
                        && o.CurrentStatus.StatusTypeId != 1
                        && !string.IsNullOrEmpty(o.Location))
                    .ToListAsync();

                var nearbyObstacles = FindNearbyObstacles(obstacles, latitude, longitude, radiusMeters);

                return Json(new
                {
                    success = true,
                    count = nearbyObstacles.Count,
                    obstacles = nearbyObstacles.OrderBy(o => o.distance).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicates");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Hjelpe-metoder

        // Henter innlogget brukers ID
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        // Oppretter en ny ObstacleStatus
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

        // Validerer fullstendig hindringregistrering
        private void ValidateObstacleRegistration(RegisterObstacleViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ObstacleName))
                ModelState.AddModelError("ObstacleName", "Obstacle name is required");

            if (!model.ObstacleHeight.HasValue || model.ObstacleHeight <= 0)
                ModelState.AddModelError("ObstacleHeight", "Height must be greater than 0");

            if (string.IsNullOrWhiteSpace(model.ObstacleDescription))
                ModelState.AddModelError("ObstacleDescription", "Description is required");

            if (string.IsNullOrWhiteSpace(model.ObstacleGeometry))
                ModelState.AddModelError("ObstacleGeometry", "Please mark the location on the map");
        }

        // Validerer fullføring av hurtigregistrering
        private void ValidateQuickRegCompletion(CompleteQuickRegViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ObstacleName))
                ModelState.AddModelError("ObstacleName", "Obstacle name is required");

            if (model.ObstacleHeight <= 0)
                ModelState.AddModelError("ObstacleHeight", "Height must be greater than 0");

            if (string.IsNullOrWhiteSpace(model.ObstacleDescription))
                ModelState.AddModelError("ObstacleDescription", "Description is required");
        }

        // Setter hindringtype, oppretter ny type hvis "Other" er valgt
        private async Task SetObstacleTypeAsync(Obstacle obstacle, string? obstacleType, string? customType)
        {
            if (obstacleType == "Other" && !string.IsNullOrWhiteSpace(customType))
            {
                var existingType = await _context.ObstacleTypes
                    .FirstOrDefaultAsync(ot => ot.Name == customType);

                if (existingType != null)
                {
                    obstacle.ObstacleTypeId = existingType.Id;
                }
                else
                {
                    var newType = new ObstacleType
                    {
                        Name = customType,
                        Description = "Custom type added by user",
                        MinHeight = 1,
                        MaxHeight = 1000
                    };
                    _context.ObstacleTypes.Add(newType);
                    await _context.SaveChangesAsync();
                    obstacle.ObstacleTypeId = newType.Id;
                }
            }
            else if (!string.IsNullOrWhiteSpace(obstacleType))
            {
                var type = await _context.ObstacleTypes
                    .FirstOrDefaultAsync(ot => ot.Name == obstacleType);

                if (type != null)
                    obstacle.ObstacleTypeId = type.Id;
            }
        }

        // Oppdaterer hindringsstatus og deaktiverer forrige status
        private async Task UpdateObstacleStatusAsync(Obstacle obstacle, int newStatusTypeId, string userId, string comments)
        {
            // Deaktiver gammel status
            if (obstacle.CurrentStatus != null)
            {
                obstacle.CurrentStatus.IsActive = false;
                _context.ObstacleStatuses.Update(obstacle.CurrentStatus);
            }

            // Opprett ny status
            var newStatus = CreateObstacleStatus(obstacle.Id, newStatusTypeId, userId, comments);
            _context.ObstacleStatuses.Add(newStatus);
            await _context.SaveChangesAsync();

            // Oppdater CurrentStatusId
            obstacle.CurrentStatusId = newStatus.Id;
            _context.Obstacles.Update(obstacle);
            await _context.SaveChangesAsync();
        }

        // Bygger ViewModel for MyRegistrations med gruppering per status
        private MyRegistrationsViewModel BuildMyRegistrationsViewModel(List<Obstacle> obstacles)
        {
            var viewModel = new MyRegistrationsViewModel();

            foreach (var obstacle in obstacles)
            {
                var statusName = obstacle.CurrentStatus?.StatusType?.Name ?? "Unknown";

                // Ufullstendige Quick Registrations
                if (statusName == "Registered" && string.IsNullOrEmpty(obstacle.Name))
                {
                    viewModel.IncompleteQuickRegs.Add(new IncompleteQuickRegItem
                    {
                        Id = obstacle.Id,
                        Location = obstacle.Location,
                        RegisteredDate = obstacle.RegisteredDate
                    });
                }
                else if (statusName == "Pending")
                {
                    viewModel.PendingObstacles.Add(MapToListItem(obstacle));
                }
                else if (statusName == "Approved")
                {
                    viewModel.ApprovedObstacles.Add(MapToListItem(obstacle));
                }
                else if (statusName == "Rejected")
                {
                    viewModel.RejectedObstacles.Add(MapToListItem(obstacle));
                }
            }

            return viewModel;
        }

        // Mapper Obstacle til ObstacleListItemViewModel
        private ObstacleListItemViewModel MapToListItem(Obstacle obstacle)
        {
            var statusName = obstacle.CurrentStatus?.StatusType?.Name ?? "Unknown";

            var item = new ObstacleListItemViewModel
            {
                Id = obstacle.Id,
                Name = obstacle.Name ?? "Unnamed",
                Height = obstacle.Height ?? 0,
                Type = obstacle.ObstacleType?.Name,
                Location = obstacle.Location,
                RegisteredDate = obstacle.RegisteredDate,
                RegisteredBy = obstacle.RegisteredByUser?.Email ?? "Unknown",
                CurrentStatus = statusName,
                IsIncomplete = statusName == "Registered" && string.IsNullOrEmpty(obstacle.Name),
                IsPending = statusName == "Pending",
                IsApproved = statusName == "Approved",
                IsRejected = statusName == "Rejected"
            };

            // Legg til behandlingsinformasjon hvis tilgjengelig
            if (obstacle.CurrentStatus != null)
            {
                item.ProcessedBy = obstacle.CurrentStatus.ChangedByUser?.Email;
                item.ProcessedDate = obstacle.CurrentStatus.ChangedDate;
                item.RejectionReason = obstacle.CurrentStatus.Comments;
            }

            return item;
        }

        // Bygger ObstacleDetailsViewModel med full hindringinformasjon
        private ObstacleDetailsViewModel BuildObstacleDetailsViewModel(Obstacle obstacle)
        {
            var viewModel = new ObstacleDetailsViewModel
            {
                Id = obstacle.Id,
                Name = obstacle.Name ?? "Unnamed",
                Height = obstacle.Height ?? 0,
                Description = obstacle.Description ?? "No description",
                Type = obstacle.ObstacleType?.Name,
                Location = obstacle.Location,
                RegisteredDate = obstacle.RegisteredDate,
                RegisteredBy = obstacle.RegisteredByUser?.Email ?? "Unknown",
                CurrentStatus = obstacle.CurrentStatus?.StatusType?.Name ?? "Unknown",
                IsPending = obstacle.CurrentStatus?.StatusType?.Name == "Pending",
                IsApproved = obstacle.CurrentStatus?.StatusType?.Name == "Approved",
                IsRejected = obstacle.CurrentStatus?.StatusType?.Name == "Rejected"
            };

            if (obstacle.CurrentStatus != null)
            {
                viewModel.ProcessedBy = obstacle.CurrentStatus.ChangedByUser?.Email;
                viewModel.ProcessedDate = obstacle.CurrentStatus.ChangedDate;
                viewModel.ProcessComments = obstacle.CurrentStatus.Comments;

                // Hvis status er Rejected (ID 4), er kommentaren avslagsårsaken
                if (obstacle.CurrentStatus.StatusTypeId == 4)
                {
                    viewModel.RejectionReason = obstacle.CurrentStatus.Comments;
                }
            }

            if (obstacle.StatusHistory != null)
            {
                viewModel.StatusHistory = obstacle.StatusHistory
                    .OrderBy(sh => sh.ChangedDate)
                    .Select(sh => new StatusHistoryItem
                    {
                        Status = sh.StatusType?.Name ?? "Unknown",
                        ChangedBy = sh.ChangedByUser?.Email ?? "Unknown",
                        ChangedDate = sh.ChangedDate,
                        Comments = sh.Comments
                    })
                    .ToList();
            }

            return viewModel;
        }

        // Sletter hindring og alle relaterte data (statuser og behandlinger)
        private async Task DeleteObstacleAndRelatedDataAsync(Obstacle obstacle)
        {
            // Fjern foreign key referanse
            obstacle.CurrentStatusId = null;
            _context.Obstacles.Update(obstacle);
            await _context.SaveChangesAsync();

            // Slett alle relaterte statuser
            var statuses = await _context.ObstacleStatuses
                .Where(s => s.ObstacleId == obstacle.Id)
                .ToListAsync();
            _context.ObstacleStatuses.RemoveRange(statuses);

            await _context.SaveChangesAsync();

            // Slett obstacle
            _context.Obstacles.Remove(obstacle);
            await _context.SaveChangesAsync();
        }

        // Henter statistikk for pilot (antall hindringer per status)
        private async Task<PilotStatistics> GetPilotStatisticsAsync(string userId)
        {
            var incomplete = await _context.Obstacles
                .Where(o => o.RegisteredByUserId == userId && o.CurrentStatusId == 1)
                .CountAsync();

            var pending = await _context.Obstacles
                .Where(o => o.RegisteredByUserId == userId && o.CurrentStatusId == 2)
                .CountAsync();

            var approved = await _context.Obstacles
                .Where(o => o.RegisteredByUserId == userId && o.CurrentStatusId == 3)
                .CountAsync();

            var rejected = await _context.Obstacles
                .Where(o => o.RegisteredByUserId == userId && o.CurrentStatusId == 4)
                .CountAsync();

            return new PilotStatistics
            {
                Incomplete = incomplete,
                Pending = pending,
                Approved = approved,
                Rejected = rejected,
                Total = incomplete + pending + approved + rejected
            };
        }

        // Finner hindringer innenfor angitt radius fra gitt posisjon
        // Bruker Haversine-formelen for å beregne avstand
        private List<dynamic> FindNearbyObstacles(
            List<Obstacle> obstacles,
            double latitude,
            double longitude,
            double radiusMeters)
        {
            var nearby = new List<dynamic>();

            foreach (var obstacle in obstacles)
            {
                var coords = ParseWktPoint(obstacle.Location);
                if (coords == null)
                    continue;

                var distance = CalculateDistance(latitude, longitude, coords.Value.lat, coords.Value.lng);

                if (distance <= radiusMeters)
                {
                    nearby.Add(new
                    {
                        id = obstacle.Id,
                        name = obstacle.Name ?? "Unnamed",
                        type = obstacle.ObstacleType?.Name ?? "Unknown",
                        height = obstacle.Height ?? 0,
                        description = obstacle.Description ?? "No description",
                        distance = Math.Round(distance, 1)
                    });
                }
            }

            return nearby;
        }

        // Parser WKT POINT-string til koordinater
        private (double lat, double lng)? ParseWktPoint(string? wkt)
        {
            if (string.IsNullOrEmpty(wkt))
                return null;

            try
            {
                // Format: POINT(longitude latitude)
                var match = System.Text.RegularExpressions.Regex.Match(
                    wkt,
                    @"POINT\s*\(\s*([\d\.\-]+)\s+([\d\.\-]+)\s*\)");

                if (match.Success)
                {
                    var lng = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var lat = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    return (lat, lng);
                }
            }
            catch { }

            return null;
        }

        // Beregner avstand mellom to koordinater med Haversine-formelen
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusMeters = 6371000;

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMeters * c;
        }

        // Konverterer grader til radianer
        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        // Hjelpeklasser

        private class PilotStatistics
        {
            public int Incomplete { get; set; }
            public int Pending { get; set; }
            public int Approved { get; set; }
            public int Rejected { get; set; }
            public int Total { get; set; }
        }
    }
}
