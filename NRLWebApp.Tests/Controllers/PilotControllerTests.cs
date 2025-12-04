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
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Trengs for ITempDataProvider

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

            var httpContext = new DefaultHttpContext() { User = user };
            var controllerContext = new ControllerContext() { HttpContext = httpContext };

            var controller = new PilotController(context, _mockLogger.Object)
            {
                ControllerContext = controllerContext,
                // Initialiser TempData for å unngå NullReferenceException i testene
                TempData = new TempDataDictionary(
                    httpContext,
                    Mock.Of<ITempDataProvider>()
                )
            };

            return controller;
        }

        // Hjelpefunksjon for å legge til StatusType-data i In-Memory DB
        private async Task SeedStatusTypes(ApplicationDbContext context)
        {
            if (!context.StatusTypes.Any())
            {
                // Vi trenger Approved (3) og Pending (2) for disse testene
                context.StatusTypes.Add(new StatusType { Id = 3, Name = "Approved" });
                context.StatusTypes.Add(new StatusType { Id = 2, Name = "Pending" });
                context.StatusTypes.Add(new StatusType { Id = 1, Name = "Registered" }); // For QuickReg
                await context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task DeleteRegistration_OtherUsersObstacle_ReturnsErrorMessage()
        {
            // Arrange
            var context = TestDbContext.Create();
            // Vi trenger Pending status (ID 2) for at sletting skal være mulig i kontroller-logikken
            await SeedStatusTypes(context);

            var otherUserId = "annen-pilot-id-999";
            var currentUserId = "pilot-id-123";

            // 1. Opprett en hindring eid av en ANNEN bruker
            var obstacle = new Obstacle
            {
                Id = 200,
                RegisteredByUserId = otherUserId, // Eies av annen bruker!
                Name = "Other Users Obstacle",
                Location = "POINT(10 10)",
                RegisteredDate = DateTime.Now
            };
            context.Obstacles.Add(obstacle);

            // 2. Opprett en slettbar status (Pending ID 2) og koble den
            var status = new ObstacleStatus
            {
                Id = 1,
                ObstacleId = 200,
                StatusTypeId = 2, // Pending
                ChangedByUserId = otherUserId,
                IsActive = true
            };
            context.ObstacleStatuses.Add(status);
            obstacle.CurrentStatusId = status.Id; // Link status til hindringen

            await context.SaveChangesAsync();

            // 3. Opprett kontrolleren som den NÅVÆRENDE brukeren ("pilot-id-123")
            var controller = CreateController(context, currentUserId);


            // Act
            // Prøver å slette hindringen (ID 200) som eies av en annen bruker (IDOR-sjekk)
            var result = await controller.DeleteRegistration(200);

            // Assert
            // Skal redirecte til MyRegistrations
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyRegistrations", redirectToActionResult.ActionName);

            // Sjekk feilmelding: Obstacle not found fordi Where-klausulen (o.RegisteredByUserId == userId) feilet
            Assert.Contains("Obstacle not found.", controller.TempData["ErrorMessage"]?.ToString());

            // Sjekk at hindringen IKKE er slettet fra databasen
            Assert.NotNull(await context.Obstacles.FindAsync(200L));
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

            // Lagre endringene for å sikre at statusen og hindringen eksisterer i DB og har en ID
            await context.SaveChangesAsync();

            // Sett CurrentStatusId og lagre endringen på den sporet entiteten.
            obstacle.CurrentStatusId = approvedStatus.Id;
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId);

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