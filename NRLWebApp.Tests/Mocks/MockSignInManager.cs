using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Threading.Tasks;

namespace NRLWebApp.Tests.Mocks
{
    public static class MockSignInManager
    {
        public static Mock<SignInManager<ApplicationUser>> Create(Mock<UserManager<ApplicationUser>> userManagerMock)
        {
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            var mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                userManagerMock.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null!, null!, null!, null!);

            // Simuler at pålogging lykkes som standard
            mockSignInManager
                .Setup(sm => sm.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);

            mockSignInManager
                .Setup(sm => sm.SignOutAsync())
                .Returns(Task.CompletedTask);

            return mockSignInManager;
        }
    }
}