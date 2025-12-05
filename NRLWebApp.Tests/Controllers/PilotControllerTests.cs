using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class PilotControllerTests
    {
        [Fact]
        public async Task QuickRegister_ValidData_SavesToDatabase()
        {
            // Arrange
            var context = TestDbContext.Create();
            var mockLogger = new Mock<ILogger<PilotController>>();

            var userId = "test-pilot-123";
            var user = new ApplicationUser { Id = userId };

            var controller = new PilotController(context, mockLogger.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            };

            // Act
            var result = await controller.QuickRegister("POINT(10 10)");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("RegisterType", redirect.ActionName);

            var obstacle = await context.Obstacles.FirstOrDefaultAsync();
            Assert.NotNull(obstacle);
            Assert.Equal("POINT(10 10)", obstacle.Location);
        }

        [Fact]
        public async Task DeleteRegistration_RemovesObstacle_And_History()
        {
            // Arrange
            var context = TestDbContext.Create();
            var mockLogger = new Mock<ILogger<PilotController>>();
            var userId = "pilot-1";

            // --- FIX START: Legg til StatusTyper slik at koden finner navnene "Pending" etc. ---
            context.StatusTypes.AddRange(
                new StatusType { Id = 1, Name = "Registered" },
                new StatusType { Id = 2, Name = "Pending" }
            );
            // --- FIX END ---

            var obstacle = new Obstacle { Id = 10L, RegisteredByUserId = userId, Location = "POINT(1 1)", Name = "To Be Deleted" };
            context.Obstacles.Add(obstacle);

            var status = new ObstacleStatus { Id = 100L, ObstacleId = 10L, StatusTypeId = 2, ChangedByUserId = userId };
            context.ObstacleStatuses.Add(status);

            obstacle.CurrentStatusId = 100L;
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var controller = new PilotController(context, mockLogger.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            };

            // Act
            var result = await controller.DeleteRegistration(10L);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyRegistrations", redirect.ActionName);
            Assert.Equal("Obstacle deleted successfully.", controller.TempData["SuccessMessage"]);

            var deletedObstacle = await context.Obstacles.FindAsync(10L);
            Assert.Null(deletedObstacle);
        }
    }
}