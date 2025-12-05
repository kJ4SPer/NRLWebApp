using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class RegisterforerControllerTests
    {
        [Fact]
        public async Task PendingObstacles_Returns_Only_Pending_Items()
        {
            // Arrange
            var context = TestDbContext.Create();
            var mockLogger = new Mock<ILogger<RegisterforerController>>();

            // 1. Opprett Dummy Bruker (For foreign keys)
            var user = new ApplicationUser { Id = "testuser", Email = "test@test.com", IsApproved = true };
            context.Users.Add(user);

            // 2. Opprett StatusTyper (Kritisk for filtreringen)
            context.StatusTypes.AddRange(
                new StatusType { Id = 2, Name = "Pending" },
                new StatusType { Id = 3, Name = "Approved" }
            );

            // 3. Opprett Hindringer (Obstacles)
            // Merk: Vi fyller ut ALLE required fields
            var obs1 = new Obstacle
            {
                Id = 100L,
                Name = "Skal Vises",
                Location = "POINT(10 10)",
                RegisteredByUserId = "testuser",
                CurrentStatusId = 1000L // Kobler til statusen vi lager under
            };

            var obs2 = new Obstacle
            {
                Id = 200L,
                Name = "Skal Ikke Vises",
                Location = "POINT(20 20)",
                RegisteredByUserId = "testuser",
                CurrentStatusId = 2000L
            };
            context.Obstacles.AddRange(obs1, obs2);

            // 4. Opprett Status-historikk
            // Status 2 = Pending (Denne skal fanges opp)
            var status1 = new ObstacleStatus
            {
                Id = 1000L,
                ObstacleId = 100L,
                StatusTypeId = 2, // <--- DETTE ER NØKKELEN (2 = Pending)
                IsActive = true,
                ChangedByUserId = "testuser",
                Comments = "Pending test"
            };

            // Status 3 = Approved (Denne skal ignoreres)
            var status2 = new ObstacleStatus
            {
                Id = 2000L,
                ObstacleId = 200L,
                StatusTypeId = 3,
                IsActive = true,
                ChangedByUserId = "testuser",
                Comments = "Approved test"
            };
            context.ObstacleStatuses.AddRange(status1, status2);

            await context.SaveChangesAsync();

            // 5. Nullstill ChangeTracker for å simulere en helt ny request mot databasen
            context.ChangeTracker.Clear();

            var controller = new RegisterforerController(context, mockLogger.Object);

            // Sett opp TempData for å unngå null-feil
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Act
            var result = await controller.PendingObstacles();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ObstacleListItemViewModel>>(viewResult.Model);

            // Vi forventer nøyaktig 1 treff (obs1)
            Assert.Single(model);
            Assert.Equal("Skal Vises", model[0].Name);
        }
    }
}