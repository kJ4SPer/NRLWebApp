using System.Threading.Tasks;
using FirstWebApplication.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NRLWebApp.Tests.Middleware
{
    public class CspMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsCspHeader_WithNonce()
        {
            // Arrange - Oppsett av mocks og context

            // 1. Vi bruker DefaultHttpContext som vår "mock" for selve HTTP-forespørselen.
            // Den har ferdige collections for Headers, Items, osv.
            var context = new DefaultHttpContext();

            // 2. Mock 'next' delegaten (neste steg i pipeline).
            // Vi sier bare at den skal returnere en fullført oppgave.
            RequestDelegate next = (innerContext) => Task.CompletedTask;

            // 3. Mock Loggeren siden middlewaren krever den i konstruktøren.
            var mockLogger = new Mock<ILogger<CspMiddleware>>();

            // 4. Instansier middlewaren
            var middleware = new CspMiddleware(next, mockLogger.Object);

            // Act - Kjør koden
            await middleware.InvokeAsync(context);

            // Assert - Sjekk resultatet

            // Sjekk at CSP-headeren ble lagt til
            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"),
                "Content-Security-Policy header mangler");

            var cspValue = context.Response.Headers["Content-Security-Policy"].ToString();

            // Sjekk at den er streng (default-src 'self')
            Assert.Contains("default-src 'self'", cspValue);

            // Sjekk at Nonce ble generert og lagt inn i CSP-strengen
            Assert.Contains("'nonce-", cspValue);

            // Sjekk at Nonce også ble lagt i HttpContext.Items (for bruk i Views)
            Assert.True(context.Items.ContainsKey("csp-nonce"),
                "Nonce ble ikke lagret i Context.Items");

            var nonceItem = context.Items["csp-nonce"]?.ToString();
            Assert.False(string.IsNullOrEmpty(nonceItem), "Nonce i Items er tom");

            // Verifiser at noncen i headeren matcher den i items
            Assert.Contains($"'nonce-{nonceItem}'", cspValue);
        }

        [Fact]
        public async Task InvokeAsync_AddsStandardSecurityHeaders()
        {
            // Arrange
            var context = new DefaultHttpContext();
            RequestDelegate next = (innerContext) => Task.CompletedTask;
            var mockLogger = new Mock<ILogger<CspMiddleware>>();
            var middleware = new CspMiddleware(next, mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Sjekk de andre sikkerhetsheaderne
            var headers = context.Response.Headers;

            Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
            Assert.Equal("DENY", headers["X-Frame-Options"]);
            Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"]);

            Assert.Equal("0", headers["X-XSS-Protection"]);
        }
    }
}