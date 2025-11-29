using Microsoft.AspNetCore.Identity;
using Moq;

namespace NRLWebApp.Tests.Mocks
{
    public static class MockRoleManager
    {
        public static Mock<RoleManager<IdentityRole>> Create()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            var mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                store.Object,
                null!,
                null!,
                null!,
                null!);

            // Simuler at rolle-håndtering er vellykket som standard
            mockRoleManager
                .Setup(rm => rm.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            mockRoleManager
                .Setup(rm => rm.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            return mockRoleManager;
        }
    }
}
