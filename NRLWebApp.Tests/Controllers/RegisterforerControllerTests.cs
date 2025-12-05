using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using FirstWebApplication.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NRLWebApp.Tests.Controllers
{
    /// <summary>
    /// Testklasse for RegisterforerController
    /// Tester godkjenning/avvisning av hindringer og dashboard-funksjonalitet
    /// </summary>
    public class RegisterforerControllerTests
    {
        private readonly ApplicationDbContext _testContext;
        private readonly Mock<ILogger<RegisterforerController>> _testMockLogger;
        private readonly RegisterforerController _testController;

        public RegisterforerControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _testContext = new ApplicationDbContext(options);

            _testMockLogger = new Mock<ILogger<RegisterforerController>>();
            _testController = new RegisterforerController(_testContext, _testMockLogger.Object);

            SeedStatusTypes();
        }

        /// <summary>
        /// Initialiserer standard statustypes i testdatabasen
        /// </summary>
        private void SeedStatusTypes()
        {
            _testContext.StatusTypes.AddRange(
                new StatusType { Id = 1, Name = "Registered" },
                new StatusType { Id = 2, Name = "Pending" },
                new StatusType { Id = 3, Name = "Approved" },
                new StatusType { Id = 4, Name = "Rejected" }
            );
            _testContext.SaveChanges();
        }

        #region Dashboard Tests

        /// <summary>
        /// Tester at dashboard returnerer korrekte tellinger av hindringer
        /// </summary>
        [Fact]
        public async Task RegisterforerDashboard_ReturnsDashboardWithCorrectStatistics()
        {
            // Arrange
            SetupObstaclesWithStatus(2, (int)ObstacleStatusEnum.Pending);
            SetupObstaclesWithStatus(3, (int)ObstacleStatusEnum.Approved);
            SetupObstaclesWithStatus(1, (int)ObstacleStatusEnum.Rejected);

            // Act
            var result = await _testController.RegisterforerDashboard();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);
        }

        #endregion

        #region Approve Obstacle Tests

        /// <summary>
        /// Tester at hindringer kan godkjennes og status oppdateres
        /// </summary>
        [Fact]
        public async Task ApproveObstacle_ValidModel_ChangesStatusToApproved()
        {
            // Arrange
            var obstacle = SetupObstacleWithStatus(1, (int)ObstacleStatusEnum.Pending);
            var model = new ApproveObstacleViewModel
            {
                ObstacleId = 1,
                Comments = "Godkjent - ingen problemer funnet"
            };

            // Act
            var result = await _testController.ApproveObstacle(model);

            // Assert
            var updatedObstacle = await _testContext.Obstacles
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == 1);

            Assert.NotNull(updatedObstacle?.CurrentStatus);
            Assert.Equal((int)ObstacleStatusEnum.Approved, updatedObstacle.CurrentStatus.StatusTypeId);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AllObstacles", redirect.ActionName);
        }

        /// <summary>
        /// Tester at godkjenningskommentarer lagres korrekt
        /// </summary>
        [Fact]
        public async Task ApproveObstacle_SavesCommentsToStatus()
        {
            // Arrange
            SetupObstacleWithStatus(2, (int)ObstacleStatusEnum.Pending);
            var model = new ApproveObstacleViewModel
            {
                ObstacleId = 2,
                Comments = "M�lt og verifisert - OK"
            };

            // Act
            await _testController.ApproveObstacle(model);

            // Assert
            var status = await _testContext.ObstacleStatuses
                .Where(s => s.ObstacleId == 2 && s.IsActive)
                .FirstOrDefaultAsync();

            Assert.NotNull(status);
            Assert.Equal("M�lt og verifisert - OK", status.Comments);
        }

        /// <summary>
        /// Tester at godkjenning av ikke-eksisterende hindring returnerer NotFound
        /// </summary>
        [Fact]
        public async Task ApproveObstacle_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var model = new ApproveObstacleViewModel { ObstacleId = 999 };

            // Act
            var result = await _testController.ApproveObstacle(model);

            // Assert
            var notFound = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        #endregion

        #region Reject Obstacle Tests

        /// <summary>
        /// Tester at hindringer kan avvises med avvisningsgrunn
        /// </summary>
        [Fact]
        public async Task RejectObstacle_ValidModel_ChangesStatusToRejected()
        {
            // Arrange
            SetupObstacleWithStatus(3, (int)ObstacleStatusEnum.Pending);
            var model = new RejectObstacleViewModel
            {
                ObstacleId = 3,
                RejectionReason = "Feil plassering",
                Comments = "Koordinater er ikke korrekte"
            };

            // Act
            var result = await _testController.RejectObstacle(model);

            // Assert
            var updatedObstacle = await _testContext.Obstacles
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == 3);

            Assert.NotNull(updatedObstacle?.CurrentStatus);
            Assert.Equal((int)ObstacleStatusEnum.Rejected, updatedObstacle.CurrentStatus.StatusTypeId);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AllObstacles", redirect.ActionName);
        }

        /// <summary>
        /// Tester at avvisningsgrunn og kommentarer kombineres i status-kommentarer
        /// </summary>
        [Fact]
        public async Task RejectObstacle_CombinesReasonAndComments()
        {
            // Arrange
            SetupObstacleWithStatus(4, (int)ObstacleStatusEnum.Pending);
            var model = new RejectObstacleViewModel
            {
                ObstacleId = 4,
                RejectionReason = "Manglende data",
                Comments = "H�yde m�tte ikke verifisert"
            };

            // Act
            await _testController.RejectObstacle(model);

            // Assert
            var status = await _testContext.ObstacleStatuses
                .Where(s => s.ObstacleId == 4 && s.IsActive)
                .FirstOrDefaultAsync();

            Assert.NotNull(status);
            Assert.Contains("Manglende data", status.Comments);
        }

        #endregion

        #region Filter Tests

        /// <summary>
        /// Tester at ventende hindringer kan filtreres
        /// </summary>
        [Fact]
        public async Task PendingObstacles_ReturnsPendingObstaclesOnly()
        {
            // Arrange
            SetupObstaclesWithStatus(2, (int)ObstacleStatusEnum.Pending);
            SetupObstaclesWithStatus(3, (int)ObstacleStatusEnum.Approved);

            // Act
            var result = await _testController.PendingObstacles();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ObstacleListItemViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        /// <summary>
        /// Tester at godkjente hindringer kan filtreres
        /// </summary>
        [Fact]
        public async Task ApprovedObstacles_ReturnsApprovedObstaclesOnly()
        {
            // Arrange
            SetupObstaclesWithStatus(5, (int)ObstacleStatusEnum.Approved);
            SetupObstaclesWithStatus(2, (int)ObstacleStatusEnum.Pending);

            // Act
            var result = await _testController.ApprovedObstacles();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ObstacleListItemViewModel>>(viewResult.Model);
            Assert.Equal(5, model.Count);
        }

        #endregion

        #region Map View Tests

        /// <summary>
        /// Tester at MapView returnerer en view
        /// </summary>
        [Fact]
        public void MapView_ReturnsViewResult()
        {
            var result = _testController.MapView();
            Assert.IsType<ViewResult>(result);
        }

        #endregion

        // ============================================================
        // HJELPEMETODER
        // ============================================================

        /// <summary>
        /// Oppretter en hindring med gitt status
        /// </summary>
        private Obstacle SetupObstacleWithStatus(long id, int statusTypeId)
        {
            var statusType = _testContext.StatusTypes.FirstOrDefault(st => st.Id == statusTypeId)
                ?? new StatusType { Id = statusTypeId, Name = "Test" };

            var status = new ObstacleStatus
            {
                Id = id,
                ObstacleId = id,
                StatusTypeId = statusTypeId,
                StatusType = statusType,
                ChangedByUserId = "admin-user",
                ChangedDate = DateTime.Now,
                IsActive = true
            };

            var obstacle = new Obstacle
            {
                Id = id,
                Name = $"Test Obstacle {id}",
                Location = """{"type":"Point","coordinates":[10.75,59.91]}""",
                RegisteredByUserId = "pilot-id",
                RegisteredDate = DateTime.Now,
                CurrentStatusId = id,
                CurrentStatus = status
            };

            _testContext.Obstacles.Add(obstacle);
            _testContext.ObstacleStatuses.Add(status);
            _testContext.SaveChanges();

            return obstacle;
        }

        /// <summary>
        /// Oppretter flere hindringer med samme status
        /// </summary>
        private void SetupObstaclesWithStatus(int count, int statusTypeId)
        {
            for (long i = 1; i <= count; i++)
            {
                SetupObstacleWithStatus(i, statusTypeId);
            }
        }
    }
}

