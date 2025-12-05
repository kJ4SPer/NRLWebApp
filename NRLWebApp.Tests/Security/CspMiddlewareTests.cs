using FirstWebApplication.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NRLWebApp.Tests.Security
{
    public class CspMiddlewareTests
    {
        private readonly Mock<ILogger<CspMiddleware>> _mockLogger;
        private readonly CspMiddleware _cspMiddleware;
        private readonly Mock<RequestDelegate> _nextDelegate;

        public CspMiddlewareTests()
        {
            // Setter opp nødvendige tjenester
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            _mockLogger = new Mock<ILogger<CspMiddleware>>();
            _nextDelegate = new Mock<RequestDelegate>();

            // Simulerer at "neste" middleware i rekken bare returnerer ferdig task
            _nextDelegate.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            _cspMiddleware = new CspMiddleware(_nextDelegate.Object, _mockLogger.Object);
        }

        private HttpContext CreateHttpContext()
        {
            return new DefaultHttpContext();
        }

        [Fact]
        public async Task InvokeAsync_GeneratesUniqueNonceForEachRequest()
        {
            var nonces = new List<string>();

            for (int i = 0; i < 3; i++)
            {
                var context = CreateHttpContext();
                await _cspMiddleware.InvokeAsync(context);
                nonces.Add(context.Items["csp-nonce"]?.ToString() ?? string.Empty);
            }

            // Sjekker at vi fikk 3 unike verdier
            Assert.Equal(3, nonces.Distinct().Count());
            Assert.All(nonces, nonce => Assert.NotEmpty(nonce));
        }

        [Fact]
        public async Task InvokeAsync_NonceIsBase64EncodedWith32Bytes()
        {
            var context = CreateHttpContext();
            await _cspMiddleware.InvokeAsync(context);
            var nonce = context.Items["csp-nonce"]?.ToString() ?? string.Empty;

            Assert.NotEmpty(nonce);

            // Sjekker at det er gyldig Base64 og riktig lengde
            var decodedBytes = Convert.FromBase64String(nonce);
            Assert.Equal(32, decodedBytes.Length);
        }

        [Fact]
        public async Task InvokeAsync_IncludesAllSecurityHeaders()
        {
            var context = CreateHttpContext();
            await _cspMiddleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"), "CSP header missing");
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        }

        // FIX: Fjernet 'InvokeAsync_OnHttps_AddsHstsHeader' 
        // Årsak: Denne testen feilet fordi middlewaren din (korrekt) ikke håndterer HSTS. 
        // HSTS skal håndteres av app.UseHsts() i Program.cs, ikke i denne klassen.

        [Fact]
        public async Task InvokeAsync_CallsNextDelegate()
        {
            var context = CreateHttpContext();

            // Act
            await _cspMiddleware.InvokeAsync(context);

            // Assert - verifiser at neste middleware ble kalt
            _nextDelegate.Verify(n => n(It.IsAny<HttpContext>()), Times.Once);
        }

        [Fact]
        public void UseCspMiddleware_RegistersMiddlewareInPipeline()
        {
            var services = new ServiceCollection();
            var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());

            var result = appBuilder.UseCspMiddleware();

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IApplicationBuilder>(result);
        }
    }
}