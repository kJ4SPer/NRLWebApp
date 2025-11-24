using System.Security.Cryptography;
using System.Text;

namespace FirstWebApplication.Middleware
{
    public class CspMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CspMiddleware> _logger;

        public CspMiddleware(RequestDelegate next, ILogger<CspMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate unique nonce for this request
            var nonce = GenerateNonce();

            // Store nonce in HttpContext for use in views
            context.Items["csp-nonce"] = nonce;

            // Build CSP header
            var cspPolicy = BuildCspPolicy(nonce, context.Request.IsHttps);

            // Add security headers
            context.Response.Headers.Append("Content-Security-Policy", cspPolicy);
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "0"); // Modern browsers use CSP instead
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Only add HSTS in production with HTTPS
            if (context.Request.IsHttps)
            {
                context.Response.Headers.Append("Strict-Transport-Security",
                    "max-age=31536000; includeSubDomains; preload");
            }

            await _next(context);
        }

        private string GenerateNonce()
        {
            // Generate cryptographically secure random nonce
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        private string BuildCspPolicy(string nonce, bool isHttps)
        {
            var policies = new List<string>
            {
                // Default: only same origin
                "default-src 'self'",

                // Scripts: self + nonce for inline scripts + CDNs
                $"script-src 'self' 'nonce-{nonce}' https://unpkg.com https://cdn.tailwindcss.com",

                // Styles: self + nonce for inline styles + CDNs
                $"style-src 'self' 'nonce-{nonce}' https://unpkg.com https://cdn.tailwindcss.com",

                // Images: self + data URIs for inline images + OpenStreetMap tiles
                "img-src 'self' data: https://*.tile.openstreetmap.org",

                // Fonts: self only
                "font-src 'self'",

                // Connect (AJAX/fetch): self for API calls
                "connect-src 'self'",

                // Media: self only
                "media-src 'self'",

                // Objects: none (block Flash, Java, etc.)
                "object-src 'none'",

                // Frame ancestors: none (prevents clickjacking)
                "frame-ancestors 'none'",

                // Base URI: self only
                "base-uri 'self'",

                // Form actions: self only
                "form-action 'self'",

                // Block all mixed content
                "block-all-mixed-content",

                // Upgrade insecure requests in production
            };

            if (isHttps)
            {
                policies.Add("upgrade-insecure-requests");
            }

            return string.Join("; ", policies) + ";";
        }
    }

    // Extension method for easy registration
    public static class CspMiddlewareExtensions
    {
        public static IApplicationBuilder UseCspMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CspMiddleware>();
        }
    }
}
