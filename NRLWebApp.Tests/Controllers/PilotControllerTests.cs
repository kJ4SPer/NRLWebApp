using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // VIKTIG FOR TEMPDATA
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
            var user = new ApplicationUser { Id = userId, IsApproved = true };

            var mockUserManager = MockHelpers.MockUserManager(new List<ApplicationUser> { user });
            mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new PilotController(context, mockUserManager.Object, mockLogger.Object);

            // --- LØSNING FOR NULLREFERENCEEXCEPTION ---
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            // ------------------------------------------

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
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
    }
}