using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using FirstWebApplication.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // VIKTIG: For TempData
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class RegisterforerControllerTests
    {
        private readonly ApplicationDbContext _testContext;
        private readonly RegisterforerController _testController;
        private readonly string _testUserId = "reg-user-id";

        public RegisterforerControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _testContext = new ApplicationDbContext(options);

            var mockLogger = new Mock<ILogger<RegisterforerController>>();
            _testController = new RegisterforerController(_testContext, mockLogger.Object);

            SeedData();
            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            // Setup User claims for loggføring
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId),
                new Claim(ClaimTypes.Name, "reg@test.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _testController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // FIX: Initialiser TempData for å unngå NullReferenceException når kontrolleren bruker det
            _testController.TempData = new TempDataDictionary(
                _testController.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );
        }

        private void SeedData()
        {
            if (!_testContext.StatusTypes.Any())
            {
                _testContext.StatusTypes.AddRange(
                    new StatusType { Id = 1, Name = "Registered" },
                    new StatusType { Id = 2, Name = "Pending" },
                    new StatusType { Id = 3, Name = "Approved" },
                    new StatusType { Id = 4, Name = "Rejected" }
                );
                _testContext.SaveChanges();
            }

            if (!_testContext.Users.Any(u => u.Id == _testUserId))
            {
                _testContext.Users.Add(new ApplicationUser { Id = _testUserId, UserName = "reg", Email = "reg@test.com" });
                _testContext.SaveChanges();
            }
        }

        [Fact]
        public async Task ApproveObstacle_ValidModel_ChangesStatusToApproved()
        {
            var obstacle = CreateObstacleWithStatus(2); // Pending
            var model = new ApproveObstacleViewModel { ObstacleId = obstacle.Id, Comments = "OK" };

            var result = await _testController.ApproveObstacle(model);

            var updated = await _testContext.Obstacles.Include(o => o.CurrentStatus).FirstOrDefaultAsync(o => o.Id == obstacle.Id);
            Assert.Equal(3, updated.CurrentStatus.StatusTypeId);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task RejectObstacle_ValidModel_ChangesStatusToRejected()
        {
            var obstacle = CreateObstacleWithStatus(2);
            var model = new RejectObstacleViewModel { ObstacleId = obstacle.Id, RejectionReason = "Feil", Comments = "Nei" };

            var result = await _testController.RejectObstacle(model);

            var updated = await _testContext.Obstacles.Include(o => o.CurrentStatus).FirstOrDefaultAsync(o => o.Id == obstacle.Id);
            Assert.Equal(4, updated.CurrentStatus.StatusTypeId);
            Assert.IsType<RedirectToActionResult>(result);
        }

        private Obstacle CreateObstacleWithStatus(int statusId)
        {
            var obs = new Obstacle
            {
                Location = "{}",
                RegisteredByUserId = _testUserId,
                RegisteredDate = DateTime.Now,
                Description = "Test"
            };
            _testContext.Obstacles.Add(obs);
            _testContext.SaveChanges();

            var status = new ObstacleStatus
            {
                ObstacleId = obs.Id,
                StatusTypeId = statusId,
                ChangedByUserId = _testUserId,
                ChangedDate = DateTime.Now,
                IsActive = true
            };
            _testContext.ObstacleStatuses.Add(status);
            _testContext.SaveChanges();

            obs.CurrentStatusId = status.Id;
            _testContext.Obstacles.Update(obs);
            _testContext.SaveChanges();

            _testContext.Entry(obs).State = EntityState.Detached;
            _testContext.Entry(status).State = EntityState.Detached;

            return obs;
        }
    }
}