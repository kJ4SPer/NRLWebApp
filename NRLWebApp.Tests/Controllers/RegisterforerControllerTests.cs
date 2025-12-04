using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class RegisterforerControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<RegisterforerController>> _mockLogger;
        private readonly RegisterforerController _controller;
        private readonly ClaimsPrincipal _user;

        public RegisterforerControllerTests()
        {
            _context = TestDbContext.Create();
            _mockLogger = new Mock<ILogger<RegisterforerController>>();

            _user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-registerforer-id"),
                new Claim(ClaimTypes.Name, "register@nrl.no"),
                new Claim(ClaimTypes.Role, "Registerfører")
            }, "mock"));

            _controller = new RegisterforerController(_context, _mockLogger.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = _user }
                },
                TempData = new Mock<ITempDataDictionary>().Object
            };
        }

        private async Task SeedDatabaseAsync()
        {
            if (!_context.StatusTypes.Any())
            {
                _context.StatusTypes.AddRange(
                    new StatusType { Id = 1, Name = "Under behandling" },
                    new StatusType { Id = 2, Name = "Sendt til godkjenning" },
                    new StatusType { Id = 3, Name = "Godkjent" },
                    new StatusType { Id = 4, Name = "Avvist" }
                );
            }
            if (!_context.ObstacleTypes.Any())
            {
                _context.ObstacleTypes.Add(new ObstacleType { Id = 1, Name = "Antenne" });
            }
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task RegisterforerDashboard_ReturnsViewWithStatistics()
        {
            await SeedDatabaseAsync();
            var obstacle = new Obstacle { Name = "Pending Obs", RegisteredDate = DateTime.Now };
            _context.Obstacles.Add(obstacle);
            await _context.SaveChangesAsync();

            _context.ObstacleStatuses.Add(new ObstacleStatus
            {
                ObstacleId = obstacle.Id,
                StatusTypeId = 2,
                IsActive = true,
                ChangedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            var result = await _controller.RegisterforerDashboard();
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(1, viewResult.ViewData["PendingCount"]);
        }

        [Fact]
        public async Task ApproveObstacle_WhenValid_UpdatesStatusAndRedirects()
        {
            await SeedDatabaseAsync();

            var obstacle = new Obstacle
            {
                Name = "Tower to Approve",
                Height = 100,
                ObstacleTypeId = 1,
                RegisteredDate = DateTime.Now
            };
            _context.Obstacles.Add(obstacle);
            await _context.SaveChangesAsync();

            var initialStatus = new ObstacleStatus
            {
                ObstacleId = obstacle.Id,
                StatusTypeId = 2,
                IsActive = true,
                ChangedDate = DateTime.Now,
                ChangedByUserId = "pilot-user"
            };
            _context.ObstacleStatuses.Add(initialStatus);
            await _context.SaveChangesAsync();

            obstacle.CurrentStatusId = initialStatus.Id;
            _context.Obstacles.Update(obstacle);
            await _context.SaveChangesAsync();

            var model = new ApproveObstacleViewModel
            {
                ObstacleId = obstacle.Id,
                Comments = "Everything looks correct."
            };

            var result = await _controller.ApproveObstacle(model);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AllObstacles", redirectResult.ActionName);

            var updatedObstacle = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == obstacle.Id);

            Assert.Equal(3, updatedObstacle?.CurrentStatus?.StatusTypeId);
        }

        [Fact]
        public async Task RejectObstacle_WhenValid_UpdatesStatusAndRedirects()
        {
            await SeedDatabaseAsync();

            var obstacle = new Obstacle
            {
                Name = "Tower to Reject",
                Height = 500,
                ObstacleTypeId = 1,
                RegisteredDate = DateTime.Now
            };
            _context.Obstacles.Add(obstacle);
            await _context.SaveChangesAsync();

            var initialStatus = new ObstacleStatus
            {
                ObstacleId = obstacle.Id,
                StatusTypeId = 2,
                IsActive = true,
                ChangedDate = DateTime.Now
            };
            _context.ObstacleStatuses.Add(initialStatus);
            await _context.SaveChangesAsync();

            obstacle.CurrentStatusId = initialStatus.Id;
            _context.Obstacles.Update(obstacle);
            await _context.SaveChangesAsync();

            var model = new RejectObstacleViewModel
            {
                ObstacleId = obstacle.Id,
                RejectionReason = "Feil koordinater",
                Comments = "Vennligst sjekk kartet."
            };

            var result = await _controller.RejectObstacle(model);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AllObstacles", redirectResult.ActionName);

            var updatedObstacle = await _context.Obstacles
                .Include(o => o.CurrentStatus)
                .FirstOrDefaultAsync(o => o.Id == obstacle.Id);

            Assert.Equal(4, updatedObstacle?.CurrentStatus?.StatusTypeId);
        }
    }
}