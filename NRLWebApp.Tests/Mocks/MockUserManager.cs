using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NRLWebApp.Tests.Mocks
{
    public static class MockUserManager
    {
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ApplicationUser
                {
                    Id = id,
                    Email = "test@test.no",
                    IsApproved = false,
                    UserName = "testuser"
                });

            mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Pilot" });

            // Viktig: Sørg for at GetUserId returnerer ID fra Claims
            mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier));

            mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ClaimsPrincipal principal) => new ApplicationUser
                {
                    Id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "default",
                    IsApproved = true
                });

            return mockUserManager;
        }

        
        public static void SetupUsersList(Mock<UserManager<ApplicationUser>> mock, List<ApplicationUser> users)
        {
            // Vi pakker listen inn i TestAsyncEnumerable slik at EF Core async metoder fungerer
            var mockAsync = new TestAsyncEnumerable<ApplicationUser>(users);
            mock.Setup(x => x.Users).Returns(mockAsync);
        }
    }
}