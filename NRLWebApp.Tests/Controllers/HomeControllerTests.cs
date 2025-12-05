using FirstWebApplication.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_Redirects_Admin_To_AdminDashboard()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockLogger.Object);

            // Simuler en bruker som er logget inn som Admin
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "AdminUser")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = controller.Index();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminDashboard", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }
    }
}