using FirstWebApplication.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace NRLWebApp.Tests.Middleware
{
    /// <summary>
    /// Testklasse for CspMiddleware
    /// Validerer sikkerhetshoder og Content Security Policy (CSP) implementasjon
    /// </summary>
    public class CspMiddlewareTests
    {
        private readonly Mock<ILogger<CspMiddleware>> _mockLogger;
        private readonly CspMiddleware _cspMiddleware;
        private readonly Mock<RequestDelegate> _nextDelegate;

        public CspMiddlewareTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            _mockLogger = new Mock<ILogger<CspMiddleware>>();
            _nextDelegate = new Mock<RequestDelegate>();
            _cspMiddleware = new CspMiddleware(_nextDelegate.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Oppretter en ny HttpContext for hver test
        /// </summary>
        private HttpContext CreateHttpContext()
        {
            return new DefaultHttpContext();
        }

        #region Nonce Tests

        /// <summary>
        /// Validerer at hver request får en unik nonce
        /// </summary>
        [Fact]
        public async Task InvokeAsync_GeneratesUniqueNonceForEachRequest()
        {
            var nonces = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                var context = CreateHttpContext();
                await _cspMiddleware.InvokeAsync(context);
                nonces.Add(context.Items["csp-nonce"]?.ToString() ?? string.Empty);
            }

            Assert.Equal(5, nonces.Distinct().Count());
            Assert.All(nonces, nonce => Assert.NotEmpty(nonce));
        }

        /// <summary>
        /// Validerer at nonce er base64-kodet og 32 bytes
        /// </summary>
        [Fact]
        public async Task InvokeAsync_NonceIsBase64EncodedWith32Bytes()
        {
            var context = CreateHttpContext();
            await _cspMiddleware.InvokeAsync(context);
            var nonce = context.Items["csp-nonce"]?.ToString() ?? string.Empty;

            Assert.NotEmpty(nonce);
            var decodedBytes = Convert.FromBase64String(nonce);
            Assert.Equal(32, decodedBytes.Length);
        }

        #endregion

        #region CSP Header Tests

        /// <summary>
        /// Validerer at alle kritiske CSP direktiver er satt
        /// </summary>
        [Fact]
        public async Task InvokeAsync_IncludesAllRequiredCspDirectives()
        {
            var context = CreateHttpContext();
            await _cspMiddleware.InvokeAsync(context);
            var cspHeader = context.Response.Headers["Content-Security-Policy"].ToString();

            Assert.Contains("default-src 'self'", cspHeader);
            Assert.Contains("script-src", cspHeader);
            Assert.Contains("style-src", cspHeader);
            Assert.Contains("img-src", cspHeader);
            Assert.Contains("object-src 'none'", cspHeader);
            Assert.Contains("frame-ancestors 'none'", cspHeader);
        }

        /// <summary>
        /// Validerer at script-src inkluderer nonce
        /// </summary>
        [Fact]
        public async Task InvokeAsync_ScriptSrcIncludesNonce()
        {
            var context = CreateHttpContext();
            await _cspMiddleware.InvokeAsync(context);
            var cspHeader = context.Response.Headers["Content-Security-Policy"].ToString();
            var nonce = context.Items["csp-nonce"]?.ToString();

            Assert.Contains($"'nonce-{nonce}'", cspHeader);
        }

        /// <summary>
        /// Validerer at kartflisene fra OpenStreetMap og Kartverket er tillatt
        /// </summary>
        [Fact]
        public async Task InvokeAsync_AllowsMapTileSources()
        {
            var context = CreateHttpContext();
            await _cspMiddleware.InvokeAsync(context);
            var cspHeader = context.Response.Headers["Content-Security-Policy"].ToString();

            Assert.Contains("https://*.tile.openstreetmap.org", cspHeader);
            Assert.Contains("https://cache.kartverket.no", cspHeader);
        }

        #endregion

        #region Security Headers Tests

        /// <summary>
        /// Validerer at alle sikkerhetshoder er satt korrekt
        /// </summary>
        [Fact]
        public async Task InvokeAsync_IncludesAllSecurityHeaders()
        {
            var context = CreateHttpContext();
            await _cspMiddleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
            Assert.Equal("0", context.Response.Headers["X-XSS-Protection"]);
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        }

        #endregion

        #region HTTPS Tests

        /// <summary>
        /// Validerer at HSTS-header legges til på HTTPS og upgrade-insecure-requests inkluderes
        /// </summary>
        [Fact]
        public async Task InvokeAsync_OnHttps_AddsHstsAndUpgradeInsecureRequests()
        {
            var context = CreateHttpContext();
            context.Request.Scheme = "https";
            await _cspMiddleware.InvokeAsync(context);

            var hstsHeader = context.Response.Headers["Strict-Transport-Security"].ToString();
            Assert.Contains("max-age=31536000", hstsHeader);
            Assert.Contains("includeSubDomains; preload", hstsHeader);

            var cspHeader = context.Response.Headers["Content-Security-Policy"].ToString();
            Assert.Contains("upgrade-insecure-requests", cspHeader);
        }

        /// <summary>
        /// Validerer at HSTS ikke legges til på HTTP
        /// </summary>
        [Fact]
        public async Task InvokeAsync_OnHttp_ExcludesHstsAndUpgradeInsecure()
        {
            var context = CreateHttpContext();
            context.Request.Scheme = "http";
            await _cspMiddleware.InvokeAsync(context);

            Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
            
            var cspHeader = context.Response.Headers["Content-Security-Policy"].ToString();
            Assert.DoesNotContain("upgrade-insecure-requests", cspHeader);
        }

        #endregion

        #region Pipeline Tests

        /// <summary>
        /// Validerer at middleware kaller next delegate og setter headers først
        /// </summary>
        [Fact]
        public async Task InvokeAsync_CallsNextDelegateWithHeadersAlreadySet()
        {
            var headersSetBeforeNext = false;
            var mockNext = new Mock<RequestDelegate>();
            mockNext.Setup(n => n(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    headersSetBeforeNext = ctx.Response.Headers.ContainsKey("Content-Security-Policy");
                })
                .Returns(Task.CompletedTask);

            var middleware = new CspMiddleware(mockNext.Object, _mockLogger.Object);
            var context = CreateHttpContext();

            await middleware.InvokeAsync(context);

            Assert.True(headersSetBeforeNext);
            mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Once);
        }

        #endregion

        #region Extension Method Tests

        /// <summary>
        /// Validerer at extension method registrerer middleware i pipeline
        /// </summary>
        [Fact]
        public void UseCspMiddleware_RegistersMiddlewareInPipeline()
        {
            var services = new ServiceCollection();
            var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());

            var result = appBuilder.UseCspMiddleware();

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IApplicationBuilder>(result);
        }

        #endregion
    }
}