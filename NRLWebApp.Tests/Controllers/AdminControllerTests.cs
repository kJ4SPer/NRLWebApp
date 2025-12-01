using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Required for TempDataDictionary
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using FirstWebApplication.Data;

namespace NRLWebApp.Tests.Controllers
{
    public class AdminControllerTests
    {
        // Helper to setup Controller with real TempData logic
        private FirstWebApplication.Controllers.AdminController CreateController(string loggedInUserId, List<ApplicationUser> adminUsers, Mock<UserManager<ApplicationUser>> mockUserManager, Mock<IUserRoleService> mockRoleService)
        {
            var dbContext = TestDbContext.Create();

            // Simulate RoleService returning the requested admins
            mockRoleService
                .Setup(s => s.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(adminUsers);

            // Simulate logged-in Admin user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, loggedInUserId),
                new Claim(ClaimTypes.Name, "logged@in.com"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            // Create HttpContext manually to share between ControllerContext and TempData
            var httpContext = new DefaultHttpContext() { User = user };

            // FIX: Use real TempDataDictionary instead of Mock
            // Mocks do not store values assigned to indexers by default. 
            // The real class will persist the error message for the Assert check.
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var controller = new FirstWebApplication.Controllers.AdminController(mockUserManager.Object, mockRoleService.Object, dbContext)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext
                },
                TempData = tempData // Assign the real dictionary
            };

            return controller;
        }

        [Fact]
        public async Task DeleteUser_WhenDeletingLastAdmin_ReturnsErrorAndPreventsDeletion()
        {
            // Arrange
            var userToDelete = new ApplicationUser { Id = "admin-to-delete", Email = "admin@user.com" };
            var mockUserManager = MockUserManager.Create();

            var mockRoleService = new Mock<IUserRoleService>();
            mockRoleService.Setup(s => s.GetUsersInRoleAsync("Admin")).ReturnsAsync(new List<ApplicationUser> { userToDelete });

            mockUserManager.Setup(m => m.FindByIdAsync(userToDelete.Id)).ReturnsAsync(userToDelete);
            mockUserManager.Setup(m => m.GetRolesAsync(userToDelete)).ReturnsAsync(new List<string> { "Admin" });

            // Simulate another Admin is logged in
            var controller = CreateController("other-admin-id", new List<ApplicationUser> { userToDelete }, mockUserManager, mockRoleService);

            // Act
            var result = await controller.DeleteUser(userToDelete.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminUsers", redirectToActionResult.ActionName);

            // Verify the error message exists (Safe null check added)
            Assert.Contains("Cannot delete the last Admin user!", controller.TempData["Error"]?.ToString() ?? "");

            // Verify DeleteAsync was NEVER called
            mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }
    }
}