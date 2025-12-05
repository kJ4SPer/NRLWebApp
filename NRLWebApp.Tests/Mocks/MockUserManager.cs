using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NRLWebApp.Tests.Mocks
{
    /// <summary>
    /// Mock-factory for UserManager&lt;ApplicationUser&gt;
    /// Brukes for å teste controller-logikk uten faktisk databasekall
    /// </summary>
    public static class MockUserManager
    {
        /// <summary>
        /// Oppretter en ferdigkonfigurert mock av UserManager
        /// Alle standard operasjoner (Create, Update, Delete) returnerer Success
        /// </summary>
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // Standard suksessresultater for CRUD-operasjoner
            mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Returnerer standardbruker hvis ikke oppsett av spesifikk bruker
            mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ApplicationUser
                {
                    Id = id,
                    Email = "test@test.no",
                    IsApproved = false,
                    UserName = "testuser"
                });

            // Rollerelaterte operasjoner
            mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Pilot" });

            mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(um => um.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            // Henter bruker fra ClaimsPrincipal (brukt ved autentisering)
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ClaimsPrincipal principal) =>
                    new ApplicationUser
                    {
                        Id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "default-id",
                        Email = principal.Identity?.Name ?? "test@test.no",
                        UserName = principal.Identity?.Name ?? "testuser",
                        IsApproved = true
                    });

            return mockUserManager;
        }

        /// <summary>
        /// Setter opp Users-liste (IQueryable) for testing av dashboard-statistikker
        /// som krever .AsQueryable() på UserManager.Users
        /// </summary>
        /// <param name="mock">Mocked UserManager</param>
        /// <param name="users">Liste av brukere som skal returneres</param>
        public static void SetupUsersList(Mock<UserManager<ApplicationUser>> mock, List<ApplicationUser> users)
        {
            var queryableUsers = users.AsQueryable();
            mock.Setup(x => x.Users).Returns(queryableUsers);
        }
    }
}
