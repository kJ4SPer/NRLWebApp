using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Enums;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // VIKTIG
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class PilotControllerTests
    {
        private readonly ApplicationDbContext _testContext;
        private readonly Mock<UserManager<ApplicationUser>> _testMockUserManager;
        private readonly PilotController _testController;
        private readonly string _testUserId = "pilot-user-id";

        public PilotControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _testContext = new ApplicationDbContext(options);

            _testMockUserManager = MockUserManager.Create();
            var mockLogger = new Mock<ILogger<PilotController>>();

            _testController = new PilotController(_testContext, _testMockUserManager.Object, mockLogger.Object);

            SetupControllerContext();
            SeedData();
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId),
                new Claim(ClaimTypes.Name, "pilot@test.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _testController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = principal }
            };

            // FIX: Legg til TempData
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
        }

        [Fact]
        public async Task DeleteRegistration_InvalidId_ReturnsRedirect()
        {
            var result = await _testController.DeleteRegistration(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyRegistrations", redirect.ActionName);
        }
    }
}