using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Entities;
using FirstWebApplication.Entities;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NRLWebApp.Tests.Mocks;
using Xunit;
using System.Threading.Tasks;

namespace NRLWebApp.Tests.Controllers
{
    public class AccountControllerTests
    {
        [Fact]
        public async Task Register_ValidModel_CreatesPilotAndRedirectsToPilotDashboard()
        {
            // Arrange
            var mockUserManager = MockUserManager.Create();
            var mockSignInManager = MockSignInManager.Create(mockUserManager);
            var controller = new AccountController(mockUserManager.Object, mockSignInManager.Object);

            var model = new RegisterViewModel { Email = "new@user.com", Password = "Password123", ConfirmPassword = "Password123" };

            // Act
            var result = await controller.Register(model);

            // Assert
            // 1. Verifiser at brukeren ble opprettet.
            mockUserManager.Verify(um => um.CreateAsync(It.IsAny<FirstWebApplication.Entities.ApplicationUser>(), "Password123"), Times.Once);

            // 2. Verifiser at standardrollen "Pilot" ble tildelt.
            mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<FirstWebApplication.Entities.ApplicationUser>(), "Pilot"), Times.Once);

            // 3. Sjekk riktig redirect.
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("RegisterType", redirectToActionResult.ActionName);
            Assert.Equal("Pilot", redirectToActionResult.ControllerName);
        }
    }
}