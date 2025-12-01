using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NRLWebApp.Tests.Mocks;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using FirstWebApplication.Data; // Kreves for TestDbContext

namespace NRLWebApp.Tests.Controllers
{
    public class AdminControllerTests
    {
        // Hjelpefunksjon for å sette opp Controlleren med nødvendige mocks og kontekst
        private FirstWebApplication.Controllers.AdminController CreateController(string loggedInUserId, List<ApplicationUser> adminUsers, Mock<UserManager<ApplicationUser>> mockUserManager, Mock<IUserRoleService> mockRoleService)
        {
            var dbContext = TestDbContext.Create();

            // Simuler at IUserRoleService returnerer ønsket antall Admin-brukere
            mockRoleService
                .Setup(s => s.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(adminUsers);

            // Simuler den innloggede brukeren som en Admin. Dette er kritisk for å sette ClaimTypes.NameIdentifier.
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, loggedInUserId),
                new Claim(ClaimTypes.Name, "logged@in.com"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var controller = new FirstWebApplication.Controllers.AdminController(mockUserManager.Object, mockRoleService.Object, dbContext)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            return controller;
        }

        [Fact]
        public async Task DeleteUser_WhenDeletingLastAdmin_ReturnsErrorAndPreventsDeletion()
        {
            // Arrange
            var userToDelete = new ApplicationUser { Id = "admin-to-delete", Email = "admin@user.com" };
            var mockUserManager = MockUserManager.Create();

            // FIKS: Bruk Mock av grensesnittet IUserRoleService
            var mockRoleService = new Mock<IUserRoleService>();
            mockRoleService.Setup(s => s.GetUsersInRoleAsync("Admin")).ReturnsAsync(new List<ApplicationUser> { userToDelete });

            // Simuler at brukeren som slettes er Admin
            mockUserManager.Setup(m => m.FindByIdAsync(userToDelete.Id)).ReturnsAsync(userToDelete);
            mockUserManager.Setup(m => m.GetRolesAsync(userToDelete)).ReturnsAsync(new List<string> { "Admin" });

            // Simuler en annen Admin som er innlogget (ID må være forskjellig fra userToDelete.Id)
            var controller = CreateController("other-admin-id", new List<ApplicationUser> { userToDelete }, mockUserManager, mockRoleService);

            // Act
            var result = await controller.DeleteUser(userToDelete.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminUsers", redirectToActionResult.ActionName);

            // Sjekk at feilmeldingen ble satt i TempData
            Assert.Contains("Cannot delete the last Admin user!", controller.TempData["Error"]?.ToString() ?? string.Empty);

            // Sjekk at selve slette-metoden ALDRI ble kalt (Kritisk)
            mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }
    }
}