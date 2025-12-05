using FirstWebApplication.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace NRLWebApp.Tests.Middleware
{
    public class CspMiddlewareTests
    {
        // Hjelpemetode for å slippe å gjenta oppsett
        private (CspMiddleware middleware, DefaultHttpContext context) SetupMiddleware(bool isHttps = false)
        {
            var mockLogger = new Mock<ILogger<CspMiddleware>>();
            RequestDelegate next = (HttpContext) => Task.CompletedTask;

            var middleware = new CspMiddleware(next, mockLogger.Object);
            var context = new DefaultHttpContext();

            if (isHttps)
            {
                context.Request.Scheme = "https";
                context.Request.IsHttps = true;
            }

            return (middleware, context);
        }

        [Fact]
        public async Task InvokeAsync_Adds_All_Standard_Security_Headers()
        {
            // Arrange
            var (middleware, context) = SetupMiddleware();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var headers = context.Response.Headers;
            Assert.True(headers.ContainsKey("Content-Security-Policy"), "CSP header missing");
            Assert.True(headers.ContainsKey("X-Content-Type-Options"), "X-Content-Type-Options missing");
            Assert.True(headers.ContainsKey("X-Frame-Options"), "X-Frame-Options missing");
            Assert.True(headers.ContainsKey("Referrer-Policy"), "Referrer-Policy missing");

            // Verifiser spesifikke verdier for å hindre sniffing og clickjacking
            Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
            Assert.Equal("DENY", headers["X-Frame-Options"]);
        }

        [Fact]
        public async Task InvokeAsync_Generates_Unique_Nonce_Per_Request()
        {
            // Arrange - Vi kjører to separate requests
            var (middleware, context1) = SetupMiddleware();
            var (_, context2) = SetupMiddleware(); // Samme middleware logikk, ny context

            // Act
            await middleware.InvokeAsync(context1);
            await middleware.InvokeAsync(context2);

            // Assert
            var nonce1 = context1.Items["csp-nonce"] as string;
            var nonce2 = context2.Items["csp-nonce"] as string;

            Assert.NotNull(nonce1);
            Assert.NotNull(nonce2);

            // Kritisk test: De MÅ være forskjellige
            Assert.NotEqual(nonce1, nonce2);
        }

        [Fact]
        public async Task InvokeAsync_Injects_Nonce_Into_Csp_Header_Correctly()
        {
            // Arrange
            var (middleware, context) = SetupMiddleware();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var nonce = context.Items["csp-nonce"] as string;
            var cspHeader = context.Response.Headers["Content-Security-Policy"].ToString();

            // Sjekk at headeren faktisk bruker noncen vi lagde
            Assert.Contains($"'nonce-{nonce}'", cspHeader);

            // Sjekk at vi tillater script fra self
            Assert.Contains("script-src 'self'", cspHeader);
        }

        [Fact]
        public async Task InvokeAsync_On_HTTPS_Adds_Hsts_Header()
        {
            // Arrange - Simulerer HTTPS request
            var (middleware, context) = SetupMiddleware(isHttps: true);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
            var hstsValue = context.Response.Headers["Strict-Transport-Security"].ToString();
            Assert.Contains("max-age", hstsValue);
        }

        [Fact]
        public async Task InvokeAsync_On_HTTP_Does_Not_Add_Hsts_Header()
        {
            // Arrange - Simulerer vanlig HTTP request
            var (middleware, context) = SetupMiddleware(isHttps: false);

            // Act
            await middleware.InvokeAsync(context);

            // Assert - HSTS skal IKKE være der på usikker linje
            Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
        }
    }
}