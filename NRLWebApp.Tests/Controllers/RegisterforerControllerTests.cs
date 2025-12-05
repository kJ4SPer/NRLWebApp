using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
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
    public class RegisterforerControllerTests
    {
        [Fact]
        public async Task PendingObstacles_Returns_Only_Pending_Items()
        {
            // Arrange
            var context = TestDbContext.Create();
            var mockLogger = new Mock<ILogger<RegisterforerController>>();

            var user = new ApplicationUser { Id = "testuser", Email = "test@test.com", IsApproved = true };
            context.Users.Add(user);

            context.StatusTypes.AddRange(
                new StatusType { Id = 2, Name = "Pending" },
                new StatusType { Id = 3, Name = "Approved" }
            );

            var obs1 = new Obstacle { Id = 100L, Name = "Skal Vises", Location = "POINT(10 10)", RegisteredByUserId = "testuser", CurrentStatusId = 1000L };
            var obs2 = new Obstacle { Id = 200L, Name = "Skal Ikke Vises", Location = "POINT(20 20)", RegisteredByUserId = "testuser", CurrentStatusId = 2000L };
            context.Obstacles.AddRange(obs1, obs2);

            context.ObstacleStatuses.AddRange(
                new ObstacleStatus { Id = 1000L, ObstacleId = 100L, StatusTypeId = 2, IsActive = true, ChangedByUserId = "testuser" },
                new ObstacleStatus { Id = 2000L, ObstacleId = 200L, StatusTypeId = 3, IsActive = true, ChangedByUserId = "testuser" }
            );

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var controller = new RegisterforerController(context, mockLogger.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Act
            var result = await controller.PendingObstacles();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ObstacleListItemViewModel>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Skal Vises", model[0].Name);
        }

        [Fact]
        public async Task RejectObstacle_UpdatesStatusToRejected()
        {
            // Arrange
            var context = TestDbContext.Create();
            var mockLogger = new Mock<ILogger<RegisterforerController>>();

            var user = new ApplicationUser { Id = "reg-user", Email = "reg@test.com" };
            context.Users.Add(user);

            // Lag et hinder som er Pending
            var obstacle = new Obstacle { Id = 50L, Name = "Bad Obstacle", RegisteredByUserId = "reg-user", Location = "POINT(0 0)", CurrentStatusId = 500L };
            context.Obstacles.Add(obstacle);

            context.ObstacleStatuses.Add(new ObstacleStatus { Id = 500L, ObstacleId = 50L, StatusTypeId = 2, IsActive = true, ChangedByUserId = "reg-user" });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var controller = new RegisterforerController(context, mockLogger.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Simuler innlogget bruker (siden metoden bruker GetCurrentUserId)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "reg-user") };
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) } };

            // Act
            var model = new RejectObstacleViewModel { ObstacleId = 50L, RejectionReason = "Not safe", Comments = "Fix it now" };
            var result = await controller.RejectObstacle(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AllObstacles", redirect.ActionName);

            // Sjekk at databasen ble oppdatert riktig
            var updatedObstacle = await context.Obstacles.Include(o => o.CurrentStatus).FirstOrDefaultAsync(o => o.Id == 50L);

            Assert.NotNull(updatedObstacle.CurrentStatus);
            Assert.Equal(4, updatedObstacle.CurrentStatus.StatusTypeId); // 4 = Rejected
            Assert.Contains("Not safe", updatedObstacle.CurrentStatus.Comments);
            Assert.Contains("Fix it now", updatedObstacle.CurrentStatus.Comments);
            Assert.True(updatedObstacle.CurrentStatus.IsActive);
        }
    }
}