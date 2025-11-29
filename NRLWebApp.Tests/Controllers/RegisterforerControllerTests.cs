using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace NRLWebApp.Tests.Controllers
{
    public class RegisterforerControllerTests
    {
        private readonly Mock<ILogger<RegisterforerController>> _mockLogger = new Mock<ILogger<RegisterforerController>>();

        // Hjelpefunksjon for å sette opp Controlleren med en simulert Registerfører
        private RegisterforerController CreateController(ApplicationDbContext context, string userId = "registerforer-id-456")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Registerfører")
            }, "mock"));

            var controller = new RegisterforerController(context, _mockLogger.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            // Initialize TempData to prevent NullReferenceException
            if (controller.TempData == null)
            {
                controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    controller.ControllerContext.HttpContext,
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
                );
            }

            return controller;
        }

        // Hjelpefunksjon for å legge til StatusType-data i In-Memory DB
        private async Task SeedStatusTypes(ApplicationDbContext context)
        {
            if (!context.StatusTypes.Any())
            {
                context.StatusTypes.Add(new StatusType { Id = 2, Name = "Pending" });
                context.StatusTypes.Add(new StatusType { Id = 3, Name = "Approved" });
                await context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task ApproveObstacle_PendingToApproved_StatusIsUpdatedCorrectly()
        {
            // Arrange
            var context = TestDbContext.Create();
            await SeedStatusTypes(context);
            var registerforerId = "registerforer-id-456";

            // Opprett en test-hindring i Pending (2) status
            var obstacle = new Obstacle { Id = 201, RegisteredByUserId = "pilot-id", Name = "Test Obstacle" };
            context.Obstacles.Add(obstacle);

            var initialStatus = new ObstacleStatus
            { ObstacleId = 201, StatusTypeId = 2, ChangedByUserId = "pilot-id", IsActive = true };
            context.ObstacleStatuses.Add(initialStatus);
            await context.SaveChangesAsync();

            obstacle.CurrentStatusId = initialStatus.Id;
            context.Obstacles.Update(obstacle);
            await context.SaveChangesAsync();

            var controller = CreateController(context, registerforerId);
            var model = new ApproveObstacleViewModel { ObstacleId = 201, Comments = "Approved by Registerforer" };

            // Act
            await controller.ApproveObstacle(model);

            // Assert
            // 1. Sjekk at den gamle statusen er Inaktiv (Historikk)
            var oldStatus = await context.ObstacleStatuses.FindAsync(initialStatus.Id);
            Assert.False(oldStatus?.IsActive);

            // 2. Sjekk at ny CurrentStatus er satt til Approved (3) av Registerføreren
            var updatedObstacle = await context.Obstacles
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == 201);
            Assert.NotNull(updatedObstacle?.CurrentStatus); // Sikre at CurrentStatus ikke er null før den brukes
            Assert.Equal(3, updatedObstacle!.CurrentStatus!.StatusTypeId);
            Assert.True(updatedObstacle.CurrentStatus.IsActive);
            Assert.Equal(registerforerId, updatedObstacle.CurrentStatus.ChangedByUserId);
        }
    }
}