using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace NRLWebApp.Tests.Controllers
{
    public class PilotControllerTests
    {
        private readonly Mock<ILogger<PilotController>> _mockLogger = new Mock<ILogger<PilotController>>();

        // Hjelpefunksjon for å sette opp Controlleren med en simulert Pilot
        private PilotController CreateController(ApplicationDbContext context, string userId = "pilot-id-123")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Pilot")
            }, "mock"));

            var controller = new PilotController(context, _mockLogger.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            return controller;
        }

        // Hjelpefunksjon for å legge til StatusType-data i In-Memory DB
        private async Task SeedStatusTypes(ApplicationDbContext context)
        {
            if (!context.StatusTypes.Any())
            {
                // Vi trenger Approved (3) for denne testen
                context.StatusTypes.Add(new StatusType { Id = 3, Name = "Approved" });
                context.StatusTypes.Add(new StatusType { Id = 2, Name = "Pending" });
                await context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task DeleteRegistration_CannotDeleteApprovedObstacle_ReturnsError()
        {
            // Arrange
            var context = TestDbContext.Create();
            await SeedStatusTypes(context);
            var userId = "pilot-id-123";

            // Opprett en Approved (3) obstacle
            var obstacle = new Obstacle
            { Id = 102, RegisteredByUserId = userId, Name = "Approved Obstacle" };
            context.Obstacles.Add(obstacle);

            var approvedStatus = new ObstacleStatus
            { ObstacleId = 102, StatusTypeId = 3, ChangedByUserId = "admin-id", IsActive = true, StatusType = context.StatusTypes.Find(3) };
            context.ObstacleStatuses.Add(approvedStatus);

            obstacle.CurrentStatusId = approvedStatus.Id;
            context.Obstacles.Update(obstacle);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId);

            // Unngå CS8602
            if (controller.TempData == null)
            {
                controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    controller.ControllerContext.HttpContext,
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
                );
            }

            // Act
            var result = await controller.DeleteRegistration(102);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyRegistrations", redirectToActionResult.ActionName);

            // Sjekk feilmelding 
            Assert.Contains("You can only delete obstacles that are pending or incomplete.", controller.TempData["ErrorMessage"]?.ToString());

            // Sjekk at hindringen IKKE er slettet
            Assert.NotNull(await context.Obstacles.FindAsync(102L));
        }
    }
}