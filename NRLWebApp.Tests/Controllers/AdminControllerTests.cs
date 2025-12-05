using FirstWebApplication.Controllers;
using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly ApplicationDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _mockUserManager = MockUserManager.Create();
            _mockRoleManager = MockRoleManager.GetMockRoleManager();

            _controller = new AdminController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task AdminDashboard_ReturnsCounts()
        {
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", IsApproved = false, Email = "pending@test.no" },
                new ApplicationUser { Id = "2", IsApproved = true, Email = "approved@test.no" }
            };
            // FIX: Bruk helper metoden
            MockUserManager.SetupUsersList(_mockUserManager, users);

            var result = await _controller.AdminDashboard();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);
        }

        [Fact]
        public async Task EditUser_Get_ReturnsModelWithUserData()
        {
            var user = new ApplicationUser { Id = "user1", Email = "test@test.no", UserName = "testuser" };
            var users = new List<ApplicationUser> { user };

            // FIX: Bruk helper metoden her også, ellers feiler FirstOrDefaultAsync
            MockUserManager.SetupUsersList(_mockUserManager, users);
            _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

            var result = await _controller.EditUser("user1");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<EditUserViewModel>(viewResult.Model);
            Assert.Equal("user1", model.Id);
        }

        [Fact]
        public async Task AdminUsers_ReturnsAllUsers()
        {
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "user1@test.no", UserName = "user1" },
                new ApplicationUser { Id = "2", Email = "user2@test.no", UserName = "user2" }
            };
            // FIX: Bruk helper metoden
            MockUserManager.SetupUsersList(_mockUserManager, users);

            var result = await _controller.AdminUsers();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<UserViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        // ... Ta med de andre enkle testene (ApproveUser, RejectUser, DeleteUser) her ...
        // De feilet ikke i loggen din, så jeg kortet dem ned for lesbarhet.
    }
}