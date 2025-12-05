using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // VIKTIG FOR TEMPDATA
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public async Task AdminDashboard_Counts_Pending_And_TotalUsers_Correctly()
        {
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", IsApproved = false },
                new ApplicationUser { Id = "2", IsApproved = true },
                new ApplicationUser { Id = "3", IsApproved = true }
            };

            var mockUserManager = MockHelpers.MockUserManager(users);
            var mockRoleManager = MockHelpers.MockRoleManager();
            var context = TestDbContext.Create();

            var controller = new AdminController(mockUserManager.Object, mockRoleManager.Object, context);

            var result = await controller.AdminDashboard();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(1, viewResult.ViewData["PendingCount"]);
            Assert.Equal(3, viewResult.ViewData["TotalUsers"]);
        }

        [Fact]
        public async Task ApproveUser_UpdatesUser_And_Redirects()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", Email = "test@test.com", IsApproved = false };
            var usersList = new List<ApplicationUser> { user };

            var mockUserManager = MockHelpers.MockUserManager(usersList);
            mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

            var controller = new AdminController(mockUserManager.Object, MockHelpers.MockRoleManager().Object, TestDbContext.Create());

            // --- LØSNING FOR NULLREFERENCEEXCEPTION ---
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            // ------------------------------------------

            // Act
            var result = await controller.ApproveUser("user1");

            // Assert
            Assert.True(user.IsApproved);
            mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminPendingUsers", redirectResult.ActionName);
        }
    }
}