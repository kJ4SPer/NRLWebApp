using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public async Task AdminDashboard_ReturnsCorrectStatistics()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "pilot@test.com" },
                new ApplicationUser { Id = "2", Email = "admin@test.com" },
                new ApplicationUser { Id = "3", Email = "reg@test.com" }
            };

            var mockUserManager = MockHelpers.MockUserManager(users);

            // Mock IUserRoleService
            var mockRoleService = new Mock<IUserRoleService>();

            mockRoleService.Setup(s => s.GetUsersInRoleAsync("Pilot"))
                .ReturnsAsync(new List<ApplicationUser> { users[0] });

            mockRoleService.Setup(s => s.GetUsersInRoleAsync("Registerfører"))
                .ReturnsAsync(new List<ApplicationUser> { users[2] });

            mockRoleService.Setup(s => s.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<ApplicationUser> { users[1] });

            var context = TestDbContext.Create();

            var controller = new AdminController(mockUserManager.Object, mockRoleService.Object, context);

            // Act
            var result = await controller.AdminDashboard();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal(3, viewResult.ViewData["TotalUsers"]);
            Assert.Equal(1, viewResult.ViewData["PilotCount"]);
            Assert.Equal(1, viewResult.ViewData["RegisterforerCount"]);
            Assert.Equal(1, viewResult.ViewData["AdminCount"]);
        }

        [Fact]
        public async Task DeleteUser_DeletesUser_WhenFound()
        {
            // Arrange
            var user = new ApplicationUser { Id = "delete-me", Email = "del@test.com" };
            var mockUserManager = MockHelpers.MockUserManager(new List<ApplicationUser> { user });
            mockUserManager.Setup(x => x.FindByIdAsync("delete-me")).ReturnsAsync(user);

            // FIX 1: Bruk Mock<IUserRoleService> (ikke RoleManager)
            var mockRoleService = new Mock<IUserRoleService>();
            // Vi må returnere minst 2 admins slik at sletting tillates (logikken sjekker om det er siste admin)
            mockRoleService.Setup(s => s.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<ApplicationUser>
                {
                    new ApplicationUser { Id = "admin1" },
                    new ApplicationUser { Id = "admin2" }
                });

            var controller = new AdminController(mockUserManager.Object, mockRoleService.Object, TestDbContext.Create());
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // FIX 2: Sett opp ControllerContext med en "falsk" innlogget bruker hilsen kristian
            // Dette hindrer NullReferenceException når koden sjekker User?.FindFirstValue
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "logged-in-admin-id") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await controller.DeleteUser("delete-me");
            // her
            // Assert
            mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminUsers", redirectResult.ActionName);
            Assert.Equal($"User {user.Email} has been deleted", controller.TempData["Message"]);
        }
    }
}