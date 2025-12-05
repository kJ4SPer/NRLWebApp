using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstWebApplication.Controllers
{
    [Authorize(Roles = "Registerfører")]
    public class RegisterforerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegisterforerController> _logger;

        public RegisterforerController(ApplicationDbContext context, ILogger<RegisterforerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Viser dashboard med statistikk over hindringer
        [HttpGet]
        public async Task<IActionResult> RegisterforerDashboard()
        {
            var statistics = await GetDashboardStatisticsAsync();

            ViewBag.PendingCount = statistics.Pending;
            ViewBag.ApprovedCount = statistics.Approved;
            ViewBag.RejectedCount = statistics.Rejected;

            return View();
        }

        // Viser liste over hindringer som venter på godkjenning
        [HttpGet]
        public async Task<IActionResult> PendingObstacles()
        {
            var pendingObstacleIds = await _context.ObstacleStatuses
                .Where(s => s.IsActive && s.StatusTypeId == 2)
                .Select(s => s.ObstacleId)
                .Distinct()
                .ToListAsync();

            var obstacles = await _context.Obstacles
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Where(o => pendingObstacleIds.Contains(o.Id))
                .ToListAsync();

            var viewModels = obstacles.Select(o => MapToListItemViewModel(o, true, false, false)).ToList();

            return View(viewModels);
        }

        // Viser liste over godkjente hindringer
        [HttpGet]
        public async Task<IActionResult> ApprovedObstacles()
        {
            var approvedObstacleIds = await _context.ObstacleStatuses
                .Where(s => s.IsActive && s.StatusTypeId == 3)
                .Select(s => s.ObstacleId)
                .Distinct()
                .ToListAsync();

            var obstacles = await _context.Obstacles
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s.ChangedByUser)
                .Where(o => approvedObstacleIds.Contains(o.Id))
                .ToListAsync();

            var viewModels = obstacles.Select(o => MapToListItemViewModel(o, false, true, false)).ToList();

            return View(viewModels);
        }

        // Viser liste over avviste hindringer
        [HttpGet]
        public async Task<IActionResult> RejectedObstacles()
        {
            var rejectedObstacleIds = await _context.ObstacleStatuses
                .Where(s => s.IsActive && s.StatusTypeId == 4)
                .Select(s => s.ObstacleId)
                .Distinct()
                .ToListAsync();

            var obstacles = await _context.Obstacles
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s.ChangedByUser)
                .Where(o => rejectedObstacleIds.Contains(o.Id))
                .ToListAsync();

            var viewModels = obstacles.Select(o => MapToListItemViewModel(o, false, false, true)).ToList();

            return View(viewModels);
        }

        // Viser detaljert side for å vurdere en hindring
        [HttpGet]
        public async Task<IActionResult> ReviewObstacle(int? id)
        {
            if (id == null)
                return NotFound();

            var obstacle = await GetObstacleWithDetailsAsync(id.Value);

            if (obstacle == null)
                return NotFound();

            var statusHistory = await GetObstacleStatusHistoryAsync(obstacle.Id);
            var viewModel = BuildObstacleDetailsViewModel(obstacle, statusHistory);

            return View(viewModel);
        }

        // Godkjenner en hindring og oppdaterer status til Approved
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveObstacle(ApproveObstacleViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("ReviewObstacle", new { id = model.ObstacleId });

            var obstacle = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == model.ObstacleId);

            if (obstacle == null)
                return NotFound();

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await UpdateObstacleStatusAsync(obstacle, 3, userId, model.Comments ?? "");

            // ENDRET: Bruker ID i meldingen siden Name er fjernet
            TempData["SuccessMessage"] = $"Obstacle #{obstacle.Id} has been approved.";
            return RedirectToAction("AllObstacles");
        }

        // Avviser en hindring og oppdaterer status til Rejected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectObstacle(RejectObstacleViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("ReviewObstacle", new { id = model.ObstacleId });

            var obstacle = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == model.ObstacleId);

            if (obstacle == null)
                return NotFound();

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var comments = model.RejectionReason;
            if (!string.IsNullOrEmpty(model.Comments))
                comments += $"\n\nAdditional: {model.Comments}";

            await UpdateObstacleStatusAsync(obstacle, 4, userId, comments);

            // ENDRET: Bruker ID i meldingen siden Name er fjernet
            TempData["SuccessMessage"] = $"Obstacle #{obstacle.Id} has been rejected.";
            return RedirectToAction("AllObstacles");
        }

        // Viser detaljert informasjon om en hindring (read-only)
        [HttpGet]
        public async Task<IActionResult> ViewObstacle(int? id)
        {
            if (id == null)
                return NotFound();

            var obstacle = await GetObstacleWithDetailsAsync(id.Value);

            if (obstacle == null)
                return NotFound();

            var statusHistory = await GetObstacleStatusHistoryAsync(obstacle.Id);
            var viewModel = BuildObstacleDetailsViewModel(obstacle, statusHistory);

            return View(viewModel);
        }

        // Viser kartvisning av alle hindringer
        [HttpGet]
        public IActionResult MapView()
        {
            return View();
        }

        // Viser alle hindringer med filtrering og sortering
        [HttpGet]
        public async Task<IActionResult> AllObstacles(string? status = null, string? sortBy = null, string? sortOrder = "desc")
        {
            try
            {
                _logger.LogInformation("AllObstacles - Status: {Status}, SortBy: {SortBy}, SortOrder: {SortOrder}",
                    status, sortBy, sortOrder);

                var query = BuildAllObstaclesQuery(status);
                query = ApplySorting(query, sortBy, sortOrder);

                var obstacles = await query.ToListAsync();
                var viewModels = obstacles.Select(o => MapToListItemViewModelWithStatus(o)).ToList();

                // Send filterparametre til view
                ViewBag.CurrentStatus = status ?? "all";
                ViewBag.CurrentSortBy = sortBy ?? "registereddate";
                ViewBag.CurrentSortOrder = sortOrder;

                _logger.LogInformation("Returning {Count} obstacles", viewModels.Count);

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AllObstacles: {Message}", ex.Message);
                return View(new List<ObstacleListItemViewModel>());
            }
        }

        // Henter hindringer for kartvisning som JSON
        [HttpGet]
        public async Task<IActionResult> GetObstaclesForMapView(string? type = null, string? status = null)
        {
            try
            {
                _logger.LogInformation("GetObstaclesForMapView - Type: {Type}, Status: {Status}", type, status);

                var query = _context.Obstacles
                    .Include(o => o.ObstacleType)
                    .Include(o => o.CurrentStatus)
                        .ThenInclude(s => s.StatusType)
                    .Include(o => o.RegisteredByUser)
                    .Where(o => o.CurrentStatusId != null)
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                _logger.LogInformation("Total obstacles with CurrentStatus: {Count}", totalCount);

                // Filtrer på type
                if (!string.IsNullOrEmpty(type) && type.ToLower() != "all")
                {
                    query = query.Where(o => o.ObstacleType != null && o.ObstacleType.Name.ToLower() == type.ToLower());
                    var typeCount = await query.CountAsync();
                    _logger.LogInformation("After type filter '{Type}': {Count}", type, typeCount);
                }

                // Filtrer på status
                if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
                {
                    query = ApplyStatusFilter(query, status);
                    var statusCount = await query.CountAsync();
                    _logger.LogInformation("After status filter '{Status}': {Count}", status, statusCount);
                }

                var obstacles = await query
                    .Select(o => new
                    {
                        id = o.Id,
                        // NAME ER FJERNET HER
                        type = o.ObstacleType != null ? o.ObstacleType.Name : "Unknown",
                        height = o.Height ?? 0,
                        description = o.Description ?? "",
                        geometry = o.Location,
                        isApproved = o.CurrentStatus != null && o.CurrentStatus.StatusTypeId == 3,
                        isRejected = o.CurrentStatus != null && o.CurrentStatus.StatusTypeId == 4,
                        isPending = o.CurrentStatus != null && o.CurrentStatus.StatusTypeId == 2,
                        statusName = o.CurrentStatus != null && o.CurrentStatus.StatusType != null
                            ? o.CurrentStatus.StatusType.Name
                            : "Unknown",
                        registeredBy = o.RegisteredByUser != null ? o.RegisteredByUser.Email : "Unknown",
                        registeredDate = o.RegisteredDate
                    })
                    .OrderByDescending(o => o.registeredDate)
                    .ToListAsync();

                _logger.LogInformation("Returning {Count} obstacles", obstacles.Count);

                if (obstacles.Any())
                {
                    var first = obstacles.First();
                    var geomPreview = string.IsNullOrEmpty(first.geometry)
                        ? "NULL"
                        : first.geometry.Substring(0, Math.Min(50, first.geometry.Length));
                    // ENDRET: Fjernet name fra logging
                    _logger.LogInformation("First obstacle: Id={Id}, Type={Type}, Geometry={Geometry}",
                        first.id, first.type, geomPreview);
                }

                var stats = new
                {
                    total = obstacles.Count,
                    approved = obstacles.Count(o => o.isApproved),
                    pending = obstacles.Count(o => o.isPending),
                    rejected = obstacles.Count(o => o.isRejected)
                };

                _logger.LogInformation("Stats - Total: {Total}, Approved: {Approved}, Pending: {Pending}, Rejected: {Rejected}",
                    stats.total, stats.approved, stats.pending, stats.rejected);

                return Json(new { obstacles, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetObstaclesForMapView: {Message}", ex.Message);
                return StatusCode(500, new { error = "Failed to load obstacles", details = ex.Message });
            }
        }

        // Hjelpe-metoder

        private string GetCurrentUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        private async Task<RegisterforerStatistics> GetDashboardStatisticsAsync()
        {
            var pending = await GetObstacleCountByStatusAsync(2);
            var approved = await GetObstacleCountByStatusAsync(3);
            var rejected = await GetObstacleCountByStatusAsync(4);

            return new RegisterforerStatistics
            {
                Pending = pending,
                Approved = approved,
                Rejected = rejected
            };
        }

        private async Task<int> GetObstacleCountByStatusAsync(int statusTypeId)
        {
            return await _context.ObstacleStatuses
                .Where(s => s.IsActive && s.StatusTypeId == statusTypeId)
                .Select(s => s.ObstacleId)
                .Distinct()
                .CountAsync();
        }

        // Mapper Obstacle til ObstacleListItemViewModel
        private ObstacleListItemViewModel MapToListItemViewModel(
            Obstacle obstacle,
            bool isPending,
            bool isApproved,
            bool isRejected)
        {
            return new ObstacleListItemViewModel
            {
                Id = obstacle.Id,
                // NAME ER FJERNET HER
                Type = obstacle.ObstacleType?.Name,
                Height = obstacle.Height ?? 0,
                Location = obstacle.Location ?? string.Empty,
                RegisteredBy = obstacle.RegisteredByUser?.Email ?? "Unknown",
                RegisteredDate = obstacle.RegisteredDate,
                ProcessedBy = obstacle.CurrentStatus?.ChangedByUser?.Email,
                ProcessedDate = obstacle.CurrentStatus?.ChangedDate,
                RejectionReason = isRejected ? obstacle.CurrentStatus?.Comments : null,
                IsPending = isPending,
                IsApproved = isApproved,
                IsRejected = isRejected
            };
        }

        // Mapper Obstacle til ObstacleListItemViewModel med automatisk statusdeteksjon
        private ObstacleListItemViewModel MapToListItemViewModelWithStatus(Obstacle obstacle)
        {
            return new ObstacleListItemViewModel
            {
                Id = obstacle.Id,
                // NAME ER FJERNET HER
                Type = obstacle.ObstacleType?.Name,
                Height = obstacle.Height ?? 0,
                Location = obstacle.Location ?? string.Empty,
                RegisteredBy = obstacle.RegisteredByUser?.Email ?? "Unknown",
                RegisteredDate = obstacle.RegisteredDate,
                ProcessedBy = obstacle.CurrentStatus?.ChangedByUser?.Email,
                ProcessedDate = obstacle.CurrentStatus?.ChangedDate,
                StatusName = obstacle.CurrentStatus?.StatusType?.Name ?? "Unknown",
                IsPending = obstacle.CurrentStatus?.StatusTypeId == 2,
                IsApproved = obstacle.CurrentStatus?.StatusTypeId == 3,
                IsRejected = obstacle.CurrentStatus?.StatusTypeId == 4,
                RejectionReason = obstacle.CurrentStatus?.StatusTypeId == 4 ? obstacle.CurrentStatus?.Comments : null
            };
        }

        private async Task<Obstacle?> GetObstacleWithDetailsAsync(long id)
        {
            return await _context.Obstacles
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s.StatusType)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s.ChangedByUser)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        private async Task<List<ObstacleStatus>> GetObstacleStatusHistoryAsync(long obstacleId)
        {
            return await _context.ObstacleStatuses
                .Where(s => s.ObstacleId == obstacleId && !s.IsActive)
                .Include(s => s.StatusType)
                .Include(s => s.ChangedByUser)
                .OrderByDescending(s => s.ChangedDate)
                .ToListAsync();
        }

        // Bygger ObstacleDetailsViewModel
        private ObstacleDetailsViewModel BuildObstacleDetailsViewModel(
            Obstacle obstacle,
            List<ObstacleStatus> statusHistory)
        {
            return new ObstacleDetailsViewModel
            {
                Id = obstacle.Id,
                // NAME ER FJERNET HER
                Type = obstacle.ObstacleType?.Name,
                Height = obstacle.Height ?? 0,
                Description = obstacle.Description ?? "",
                Location = obstacle.Location,
                RegisteredBy = obstacle.RegisteredByUser?.Email ?? "Unknown",
                RegisteredDate = obstacle.RegisteredDate,
                IsPending = obstacle.CurrentStatus?.StatusTypeId == 2,
                IsApproved = obstacle.CurrentStatus?.StatusTypeId == 3,
                IsRejected = obstacle.CurrentStatus?.StatusTypeId == 4,
                ProcessedBy = obstacle.CurrentStatus?.ChangedByUser?.Email,
                ProcessedDate = obstacle.CurrentStatus?.ChangedDate,
                ProcessComments = obstacle.CurrentStatus?.Comments,
                RejectionReason = obstacle.CurrentStatus?.StatusTypeId == 4 ? obstacle.CurrentStatus?.Comments : null,
                StatusHistory = statusHistory
                    .Select(s => new StatusHistoryItem
                    {
                        Status = s.StatusType?.Name ?? "Unknown",
                        ChangedBy = s.ChangedByUser?.Email ?? "Unknown",
                        ChangedDate = s.ChangedDate,
                        Comments = s.Comments
                    })
                    .ToList()
            };
        }

        private async Task UpdateObstacleStatusAsync(Obstacle obstacle, int statusTypeId, string userId, string comments)
        {
            if (obstacle.CurrentStatus != null)
            {
                obstacle.CurrentStatus.IsActive = false;
                _context.ObstacleStatuses.Update(obstacle.CurrentStatus);
            }

            var newStatus = new ObstacleStatus
            {
                ObstacleId = obstacle.Id,
                StatusTypeId = statusTypeId,
                ChangedByUserId = userId,
                ChangedDate = DateTime.Now,
                Comments = comments,
                IsActive = true
            };

            _context.ObstacleStatuses.Add(newStatus);
            await _context.SaveChangesAsync();

            obstacle.CurrentStatusId = newStatus.Id;
            _context.Obstacles.Update(obstacle);
            await _context.SaveChangesAsync();
        }

        private IQueryable<Obstacle> BuildAllObstaclesQuery(string? status)
        {
            var query = _context.Obstacles
                .Include(o => o.ObstacleType)
                .Include(o => o.RegisteredByUser)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s.StatusType)
                .Include(o => o.CurrentStatus)
                    .ThenInclude(s => s.ChangedByUser)
                .Where(o => o.CurrentStatusId != null)
                .Where(o => o.CurrentStatus != null && o.CurrentStatus.StatusTypeId != 1)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                query = ApplyStatusFilter(query, status);
            }

            return query;
        }

        private IQueryable<Obstacle> ApplyStatusFilter(IQueryable<Obstacle> query, string status)
        {
            return status.ToLower() switch
            {
                "pending" => query.Where(o => o.CurrentStatus != null && o.CurrentStatus.StatusTypeId == 2),
                "approved" => query.Where(o => o.CurrentStatus != null && o.CurrentStatus.StatusTypeId == 3),
                "rejected" => query.Where(o => o.CurrentStatus != null && o.CurrentStatus.StatusTypeId == 4),
                _ => query
            };
        }

        // Anvender sortering på query (Fjernet sortering på Name)
        private IQueryable<Obstacle> ApplySorting(IQueryable<Obstacle> query, string? sortBy, string? sortOrder)
        {
            return sortBy?.ToLower() switch
            {
                // "NAME" CASE ER FJERNET HERFRA
                "type" => sortOrder == "asc"
                    ? query.OrderBy(o => o.ObstacleType!.Name)
                    : query.OrderByDescending(o => o.ObstacleType!.Name),
                "height" => sortOrder == "asc"
                    ? query.OrderBy(o => o.Height)
                    : query.OrderByDescending(o => o.Height),
                "status" => sortOrder == "asc"
                    ? query.OrderBy(o => o.CurrentStatus!.StatusTypeId)
                    : query.OrderByDescending(o => o.CurrentStatus!.StatusTypeId),
                "registereddate" => sortOrder == "asc"
                    ? query.OrderBy(o => o.RegisteredDate)
                    : query.OrderByDescending(o => o.RegisteredDate),
                _ => query.OrderByDescending(o => o.RegisteredDate)
            };
        }

        private class RegisterforerStatistics
        {
            public int Pending { get; set; }
            public int Approved { get; set; }
            public int Rejected { get; set; }
        }
    }
}