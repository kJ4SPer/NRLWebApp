using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading.Tasks;

namespace NRLWebApp.Tests.Mocks
{
    public static class MockSignInManager
    {
        public static Moq.Mock<SignInManager<ApplicationUser>> Create(Moq.Mock<UserManager<ApplicationUser>> userManager = null)
        {
            userManager ??= MockUserManager.Create();

            var httpContextAccessor = new Moq.Mock<IHttpContextAccessor>();
            var claimsFactory = new Moq.Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var options = Options.Create(new IdentityOptions());
            var logger = Moq.Mock.Of<ILogger<SignInManager<ApplicationUser>>>();
            var schemes = Moq.Mock.Of<IAuthenticationSchemeProvider>();
            var confirmation = Moq.Mock.Of<IUserConfirmation<ApplicationUser>>();

            var mock = new Moq.Mock<SignInManager<ApplicationUser>>(
                userManager.Object,
                httpContextAccessor.Object,
                claimsFactory.Object,
                options,
                logger,
                schemes,
                confirmation);

            // sensible defaults used by tests, overridable per-test
            mock.Setup(s => s.PasswordSignInAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<string>(), Moq.It.IsAny<bool>(), Moq.It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Failed);
            mock.Setup(s => s.SignInAsync(Moq.It.IsAny<ApplicationUser>(), Moq.It.IsAny<bool>(), Moq.It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mock.Setup(s => s.SignOutAsync())
                .Returns(Task.CompletedTask);

            return mock;
        }
    }
}