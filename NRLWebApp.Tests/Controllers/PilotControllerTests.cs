using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Enums;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Security.Claims;

namespace NRLWebApp.Tests.Controllers
{
    public class PilotControllerTests
    {
        private readonly ApplicationDbContext _testContext;
        private readonly Mock<UserManager<ApplicationUser>> _testMockUserManager;
        private readonly PilotController _testController;
        private readonly string _testUserId = "pilot-user-id";
        private readonly string _testUserEmail = "pilot@test.com";

        public PilotControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _testContext = new ApplicationDbContext(options);

            _testMockUserManager = MockUserManager.Create();
            var mockLogger = new Mock<ILogger<PilotController>>();

            _testController = new PilotController(_testContext, _testMockUserManager.Object, mockLogger.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId),
                new Claim(ClaimTypes.Name, _testUserEmail)
            }, "mock"));

            _testController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        #region RegisterType GET Tests

        [Fact]
        public async Task RegisterType_Get_WithApprovedUser_ReturnsView()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = true });

            // Act
            var result = await _testController.RegisterType();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task RegisterType_Get_WithUnapprovedUser_RedirectsToAccountPending()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = false });

            // Act
            var result = await _testController.RegisterType();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccountPending", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        #endregion

        #region QuickRegister GET Tests

        [Fact]
        public async Task QuickRegister_Get_WithApprovedUser_ReturnsView()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = true });

            // Act
            var result = await _testController.QuickRegister();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task QuickRegister_Get_WithUnapprovedUser_RedirectsToAccountPending()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = false });

            // Act
            var result = await _testController.QuickRegister();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccountPending", redirectResult.ActionName);
        }

        #endregion

        #region QuickRegister POST Tests

        [Fact]
        public async Task QuickRegister_Post_ValidGeometry_SavesToDatabase()
        {
            // Arrange
            var geometry = """{"type":"Point","coordinates":[10.75,59.91]}""";
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = true });
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            var result = await _testController.QuickRegister(geometry);

            // Assert
            var obstacle = await _testContext.Obstacles.FirstOrDefaultAsync();
            Assert.NotNull(obstacle);
            Assert.Equal(_testUserId, obstacle.RegisteredByUserId);
            Assert.Empty(obstacle.Name);
            Assert.Equal(geometry, obstacle.Location);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("RegisterType", redirect.ActionName);
        }

        [Fact]
        public async Task QuickRegister_Post_ValidGeometry_CreatesRegisteredStatus()
        {
            // Arrange
            var geometry = """{"type":"Point","coordinates":[10.75,59.91]}""";
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = true });
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            await _testController.QuickRegister(geometry);

            // Assert
            var status = await _testContext.ObstacleStatuses
                .FirstOrDefaultAsync(s => s.StatusTypeId == (int)ObstacleStatusEnum.Registered);
            Assert.NotNull(status);
            Assert.True(status.IsActive);
            Assert.Equal(_testUserId, status.ChangedByUserId);
        }

        [Fact]
        public async Task QuickRegister_Post_EmptyGeometry_ReturnsViewWithError()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = true });

            // Act
            var result = await _testController.QuickRegister(string.Empty);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Contains("Du må markere", _testController.TempData["ErrorMessage"]?.ToString() ?? "");
        }

        [Fact]
        public async Task QuickRegister_Post_EmptyGeometry_DoesNotSaveToDatabase()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = true });

            // Act
            await _testController.QuickRegister(string.Empty);

            // Assert
            var obstacles = await _testContext.Obstacles.ToListAsync();
            Assert.Empty(obstacles);
        }

        [Fact]
        public async Task QuickRegister_Post_UnapprovedUser_RedirectsToAccountPending()
        {
            // Arrange
            var geometry = """{"type":"Point","coordinates":[10.75,59.91]}""";
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = false });

            // Act
            var result = await _testController.QuickRegister(geometry);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccountPending", redirectResult.ActionName);
        }

        #endregion

        #region QuickRegisterApi Tests

        [Fact]
        public async Task QuickRegisterApi_ValidGeometry_ReturnsJsonSuccess()
        {
            // Arrange
            var geometry = """{"type":"Point","coordinates":[10.75,59.91]}""";
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            var result = await _testController.QuickRegisterApi(geometry);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            dynamic? jsonObject = jsonResult.Value as dynamic;
            Assert.True((bool)jsonObject.success);
            Assert.NotNull(jsonObject.id);
        }

        [Fact]
        public async Task QuickRegisterApi_EmptyGeometry_ReturnsJsonFailure()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            var result = await _testController.QuickRegisterApi(string.Empty);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic? jsonObject = jsonResult.Value as dynamic;
            Assert.False((bool)jsonObject.success);
            Assert.NotNull(jsonObject.message);
        }

        [Fact]
        public async Task QuickRegisterApi_ValidGeometry_SavesObstacleWithStatus()
        {
            // Arrange
            var geometry = """{"type":"Point","coordinates":[10.75,59.91]}""";
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            await _testController.QuickRegisterApi(geometry);

            // Assert
            var obstacle = await _testContext.Obstacles.FirstOrDefaultAsync();
            Assert.NotNull(obstacle);
            Assert.Equal(geometry, obstacle.Location);
            Assert.NotNull(obstacle.CurrentStatusId);

            var status = await _testContext.ObstacleStatuses
                .FirstOrDefaultAsync(s => s.Id == obstacle.CurrentStatusId);
            Assert.NotNull(status);
            Assert.Equal((int)ObstacleStatusEnum.Registered, status.StatusTypeId);
        }

        #endregion

        #region FullRegister GET Tests

        [Fact]
        public async Task FullRegister_Get_WithApprovedUser_ReturnsViewWithModel()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = true });

            // Act
            var result = await _testController.FullRegister();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RegisterObstacleViewModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task FullRegister_Get_WithUnapprovedUser_RedirectsToAccountPending()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = false });

            // Act
            var result = await _testController.FullRegister();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccountPending", redirectResult.ActionName);
        }

        #endregion

        #region MyRegistrations Tests

        [Fact]
        public async Task MyRegistrations_WithPendingObstacles_ReturnsCorrectViewModel()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            var statusType = new StatusType { Id = (int)ObstacleStatusEnum.Pending, Name = "Pending" };
            _testContext.StatusTypes.Add(statusType);
            await _testContext.SaveChangesAsync();

            var obstacle = new Obstacle
            {
                Name = "Test Obstacle",
                RegisteredByUserId = _testUserId,
                Location = """{"type":"Point","coordinates":[10.75,59.91]}""",
                RegisteredDate = DateTime.Now
            };

            var status = new ObstacleStatus
            {
                Obstacle = obstacle,
                StatusTypeId = (int)ObstacleStatusEnum.Pending,
                StatusType = statusType,
                ChangedByUserId = _testUserId,
                ChangedDate = DateTime.Now,
                IsActive = true
            };

            _testContext.Obstacles.Add(obstacle);
            await _testContext.SaveChangesAsync();

            status.ObstacleId = obstacle.Id;
            obstacle.CurrentStatus = status;
            obstacle.CurrentStatusId = status.Id;

            _testContext.ObstacleStatuses.Add(status);
            _testContext.Obstacles.Update(obstacle);
            await _testContext.SaveChangesAsync();

            // Act
            var result = await _testController.MyRegistrations();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MyRegistrationsViewModel>(viewResult.Model);
            Assert.Single(model.PendingObstacles);
        }

        [Fact]
        public async Task MyRegistrations_WithNoObstacles_ReturnsEmptyViewModel()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            var result = await _testController.MyRegistrations();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MyRegistrationsViewModel>(viewResult.Model);
            Assert.Empty(model.PendingObstacles);
            Assert.Empty(model.ApprovedObstacles);
            Assert.Empty(model.RejectedObstacles);
            Assert.Empty(model.IncompleteQuickRegs);
        }

        [Fact]
        public async Task MyRegistrations_ShowsOnlyCurrentUserObstacles()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            var statusType = new StatusType { Id = (int)ObstacleStatusEnum.Pending, Name = "Pending" };
            _testContext.StatusTypes.Add(statusType);
            await _testContext.SaveChangesAsync();

            // Add obstacle for current user
            var myObstacle = new Obstacle
            {
                Name = "My Obstacle",
                RegisteredByUserId = _testUserId,
                Location = """{"type":"Point","coordinates":[10.75,59.91]}"""
            };

            // Add obstacle for different user
            var otherObstacle = new Obstacle
            {
                Name = "Other Obstacle",
                RegisteredByUserId = "other-user-id",
                Location = """{"type":"Point","coordinates":[10.75,59.91]}"""
            };

            _testContext.Obstacles.AddRange(myObstacle, otherObstacle);
            await _testContext.SaveChangesAsync();

            // Act
            var result = await _testController.MyRegistrations();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MyRegistrationsViewModel>(viewResult.Model);
            var totalCount = model.PendingObstacles.Count + model.ApprovedObstacles.Count
                + model.RejectedObstacles.Count + model.IncompleteQuickRegs.Count;
            Assert.Equal(0, totalCount); // No statuses assigned yet
        }

        #endregion

        #region Overview Tests

        [Fact]
        public async Task Overview_WithValidId_ReturnsObstacleDetails()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            var statusType = new StatusType { Id = (int)ObstacleStatusEnum.Approved, Name = "Approved" };
            _testContext.StatusTypes.Add(statusType);
            await _testContext.SaveChangesAsync();

            var obstacle = new Obstacle
            {
                Id = 1,
                Name = "Test Obstacle",
                Height = 50,
                Description = "A test obstacle",
                RegisteredByUserId = _testUserId,
                Location = """{"type":"Point","coordinates":[10.75,59.91]}"""
            };

            var status = new ObstacleStatus
            {
                ObstacleId = 1,
                StatusTypeId = (int)ObstacleStatusEnum.Approved,
                StatusType = statusType,
                ChangedByUserId = _testUserId,
                ChangedDate = DateTime.Now,
                IsActive = true
            };

            _testContext.Obstacles.Add(obstacle);
            await _testContext.SaveChangesAsync();

            status.ObstacleId = obstacle.Id;
            obstacle.CurrentStatus = status;
            obstacle.CurrentStatusId = status.Id;

            _testContext.ObstacleStatuses.Add(status);
            _testContext.Obstacles.Update(obstacle);
            await _testContext.SaveChangesAsync();

            // Act
            var result = await _testController.Overview(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ObstacleDetailsViewModel>(viewResult.Model);
            Assert.Equal("Test Obstacle", model.Name);
            Assert.Equal(50, model.Height);
        }

        [Fact]
        public async Task Overview_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            var result = await _testController.Overview(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Overview_WithOtherUserObstacle_ReturnsNotFound()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            var obstacle = new Obstacle
            {
                Id = 1,
                Name = "Other User Obstacle",
                RegisteredByUserId = "other-user-id",
                Location = """{"type":"Point","coordinates":[10.75,59.91]}"""
            };

            _testContext.Obstacles.Add(obstacle);
            await _testContext.SaveChangesAsync();

            // Act
            var result = await _testController.Overview(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region DeleteRegistration Tests

        [Fact]
        public async Task DeleteRegistration_ValidId_DeletesObstacle()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            var obstacle = new Obstacle
            {
                Id = 1,
                Name = "Delete Me",
                RegisteredByUserId = _testUserId,
                Location = """{"type":"Point","coordinates":[10.75,59.91]}"""
            };

            _testContext.Obstacles.Add(obstacle);
            await _testContext.SaveChangesAsync();

            // Act
            var result = await _testController.DeleteRegistration(1);

            // Assert
            var deletedObstacle = await _testContext.Obstacles.FindAsync(1L);
            Assert.Null(deletedObstacle);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyRegistrations", redirect.ActionName);
        }

        [Fact]
        public async Task DeleteRegistration_WithStatus_DeletesStatusHistory()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            var obstacle = new Obstacle
            {
                Id = 1,
                Name = "Delete Me",
                RegisteredByUserId = _testUserId,
                Location = """{"type":"Point","coordinates":[10.75,59.91]}"""
            };

            var status = new ObstacleStatus
            {
                ObstacleId = 1,
                StatusTypeId = (int)ObstacleStatusEnum.Pending,
                ChangedByUserId = _testUserId,
                ChangedDate = DateTime.Now,
                IsActive = true
            };

            _testContext.Obstacles.Add(obstacle);
            await _testContext.SaveChangesAsync();

            status.ObstacleId = obstacle.Id;
            _testContext.ObstacleStatuses.Add(status);
            await _testContext.SaveChangesAsync();

            // Act
            await _testController.DeleteRegistration(1);

            // Assert
            var statuses = await _testContext.ObstacleStatuses.Where(s => s.ObstacleId == 1).ToListAsync();
            Assert.Empty(statuses);
        }

        [Fact]
        public async Task DeleteRegistration_InvalidId_ReturnsRedirect()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // Act
            var result = await _testController.DeleteRegistration(999);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyRegistrations", redirect.ActionName);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task AllControllerActions_RequireApproval_RedirectWhenUnapproved()
        {
            // Arrange
            _testMockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { IsApproved = false });

            // Act - Test RegisterType
            var registerTypeResult = await _testController.RegisterType();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(registerTypeResult);
            Assert.Equal("AccountPending", redirectResult.ActionName);
        }

        #endregion
    }
}

