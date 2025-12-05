using Microsoft.AspNetCore.Identity;
using Moq;

namespace NRLWebApp.Tests.Mocks
{
    /// <summary>
    /// Mock-factory for RoleManager&lt;IdentityRole&gt;
    /// Brukes for å teste rollerelatert logikk uten faktisk databasekall
    /// </summary>
    public static class MockRoleManager
    {
        /// <summary>
        /// Oppretter en ferdigkonfigurert mock av RoleManager
        /// Alle standard operasjoner (Create, Delete, FindByName) returnerer Success eller gyldige objekt
        /// </summary>
        /// <returns>Mock av RoleManager</returns>
        public static Mock<RoleManager<IdentityRole>> GetMockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            var mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                store.Object, null!, null!, null!, null!);

            // Opprett rolle - returnerer alltid suksess
            mockRoleManager.Setup(rm => rm.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Slett rolle - returnerer alltid suksess
            mockRoleManager.Setup(rm => rm.DeleteAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Sjekk om rolle eksisterer - returnerer true hvis navn ikke er tomt
            mockRoleManager.Setup(rm => rm.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync((string roleName) => !string.IsNullOrEmpty(roleName));

            // Finn rolle by navn - returnerer rolle med gitt navn
            mockRoleManager.Setup(rm => rm.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string roleName) => new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName
                });

            return mockRoleManager;
        }
    }
}