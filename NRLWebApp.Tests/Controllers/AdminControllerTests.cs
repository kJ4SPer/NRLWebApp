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
using System.Security.Claims;

namespace NRLWebApp.Tests.Controllers
{
    /// <summary>
    /// Testklasse for AdminController
    /// Tester brukerbehandling, godkjenning og rollehåndtering
    /// </summary>
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly ApplicationDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Organisasjoner.Add(new Organisasjon { Id = 1, Name = "Test Organisasjon" });
            _context.SaveChanges();

            _mockUserManager = MockUserManager.Create();
            _mockRoleManager = MockRoleManager.GetMockRoleManager();

            _controller = new AdminController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        #region Dashboard Tests

        /// <summary>
        /// Tester at dashboard returnerer korrekt antall godkjente og ventende brukere
        /// </summary>
        [Fact]
        public async Task AdminDashboard_ReturnsCounts()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", IsApproved = false, Email = "pending1@test.no" },
                new ApplicationUser { Id = "2", IsApproved = true, Email = "approved@test.no" },
                new ApplicationUser { Id = "3", IsApproved = false, Email = "pending2@test.no" }
            };
            MockUserManager.SetupUsersList(_mockUserManager, users);

            // Act
            var result = await _controller.AdminDashboard();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(2, viewResult.ViewData["PendingCount"]);
            Assert.Equal(3, viewResult.ViewData["TotalUsers"]);
        }

        /// <summary>
        /// Tester at dashboard returnerer tom liste når ingen brukere eksisterer
        /// </summary>
        [Fact]
        public async Task AdminDashboard_WithNoUsers_ReturnsZeroCounts()
        {
            // Arrange
            MockUserManager.SetupUsersList(_mockUserManager, new List<ApplicationUser>());

            // Act
            var result = await _controller.AdminDashboard();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(0, viewResult.ViewData["PendingCount"]);
            Assert.Equal(0, viewResult.ViewData["TotalUsers"]);
        }

        #endregion

        #region Approve User Tests

        /// <summary>
        /// Tester at en ventende bruker kan godkjennes
        /// </summary>
        [Fact]
        public async Task ApproveUser_ValidId_SetsApprovedTrue()
        {
            // Arrange
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                IsApproved = false, 
                Email = "pending@test.no",
                UserName = "pending_user"
            };
            _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

            // Act
            var result = await _controller.ApproveUser("user1");

            // Assert
            Assert.True(user.IsApproved);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminPendingUsers", redirect.ActionName);
        }

        /// <summary>
        /// Tester at godkjenning av ikke-eksisterende bruker håndteres
        /// </summary>
        [Fact]
        public async Task ApproveUser_NonExistentId_RedirectsWithError()
        {
            // Arrange
            _mockUserManager.Setup(x => x.FindByIdAsync("nonexistent"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.ApproveUser("nonexistent");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminPendingUsers", redirect.ActionName);
        }

        #endregion

        #region Reject User Tests

        /// <summary>
        /// Tester at en ventende bruker kan avvises/slettes
        /// </summary>
        [Fact]
        public async Task RejectUser_ValidId_DeletesUser()
        {
            // Arrange
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                Email = "reject@test.no",
                UserName = "reject_user"
            };
            _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

            // Act
            var result = await _controller.RejectUser("user1");

            // Assert
            _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminPendingUsers", redirect.ActionName);
        }

        #endregion

        #region Edit User Tests

        /// <summary>
        /// Tester at brukerdata kan oppdateres med rolle-endring
        /// </summary>
        [Fact]
        public async Task EditUser_Post_UpdatesUserAndRole()
        {
            // Arrange
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                Email = "old@test.no",
                Fornavn = "Old",
                Etternavn = "Name",
                UserName = "oldname"
            };
            var model = new EditUserViewModel
            {
                Id = "user1",
                Fornavn = "Test",
                Etternavn = "User",
                Email = "new@test.no",
                OrganisasjonId = 1,
                CurrentRole = "Pilot"
            };

            _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Pilot"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.EditUser(model);

            // Assert
            Assert.Equal("Test", user.Fornavn);
            Assert.Equal("new@test.no", user.Email);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
            _mockUserManager.Verify(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(user, "Pilot"), Times.Once);
            
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminUsers", redirect.ActionName);
        }

        /// <summary>
        /// Tester at GET EditUser returnerer brukerdata i viewmodel
        /// </summary>
        [Fact]
        public async Task EditUser_Get_ReturnsModelWithUserData()
        {
            // Arrange
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                Email = "test@test.no", 
                Fornavn = "Test",
                UserName = "testuser"
            };
            var users = new List<ApplicationUser> { user }.AsQueryable();
            
            _mockUserManager.Setup(x => x.Users).Returns(users);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Pilot" });

            // Act
            var result = await _controller.EditUser("user1");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<EditUserViewModel>(viewResult.Model);
            Assert.Equal("user1", model.Id);
            Assert.Equal("test@test.no", model.Email);
            Assert.Equal("Test", model.Fornavn);
        }

        #endregion

        #region Delete User Tests

        /// <summary>
        /// Tester at en bruker kan slettes permanent
        /// </summary>
        [Fact]
        public async Task DeleteUser_ValidId_DeletesUser()
        {
            // Arrange
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                Email = "delete@test.no",
                UserName = "deleteuser"
            };
            _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DeleteUser("user1");

            // Assert
            _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminUsers", redirect.ActionName);
        }

        #endregion

        #region User List Tests

        /// <summary>
        /// Tester at alle brukere hentes med korrekt rolle-informasjon
        /// </summary>
        [Fact]
        public async Task AdminUsers_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "user1@test.no", Fornavn = "User", Etternavn = "One", UserName = "user1" },
                new ApplicationUser { Id = "2", Email = "user2@test.no", Fornavn = "User", Etternavn = "Two", UserName = "user2" }
            };
            _mockUserManager.Setup(x => x.Users).Returns(users.AsQueryable());
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Pilot" });

            // Act
            var result = await _controller.AdminUsers();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<UserViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.All(model, m => Assert.NotNull(m.Email));
        }

        /// <summary>
        /// Tester at tom brukerliste håndteres korrekt
        /// </summary>
        [Fact]
        public async Task AdminUsers_WithNoUsers_ReturnsEmptyList()
        {
            // Arrange
            var emptyUsers = new List<ApplicationUser>().AsQueryable();
            _mockUserManager.Setup(x => x.Users).Returns(emptyUsers);

            // Act
            var result = await _controller.AdminUsers();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<UserViewModel>>(viewResult.Model);
            Assert.Empty(model);
        }

        #endregion
    }
}