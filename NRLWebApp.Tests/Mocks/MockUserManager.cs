using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NRLWebApp.Tests.Mocks
{
    public static class MockUserManager
    {
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            // Simuler at brukerhåndtering er vellykket som standard
            mockUserManager
                .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager
                .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Standardoppsett for å finne en bruker (for AccountController Login)
            mockUserManager
                .Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => new ApplicationUser { Id = "test-user-id", Email = email });

            // Standardoppsett for å hente bruker etter ID (for AdminController DeleteUser)
            mockUserManager
                .Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ApplicationUser { Id = id, Email = $"user{id}@test.com" });

            // Standardrolle: Pilot
            mockUserManager
                .Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Pilot" });

            return mockUserManager;
        }
    }
}