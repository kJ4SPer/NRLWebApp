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
            // 1. Generer nonce
            var nonce = GenerateNonce();

            // 2. Lagre med riktig nøkkel ("csp-nonce")
            context.Items["csp-nonce"] = nonce;

            // 3. Bygg Policy
            var cspPolicy = BuildCspPolicy(nonce, context.Request.IsHttps);

            // 4. Headers
            context.Response.Headers.Append("Content-Security-Policy", cspPolicy);
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            await _next(context);
        }

        private string GenerateNonce()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        private string BuildCspPolicy(string nonce, bool isHttps)
        {
            // Enklere policy for å unngå blokkeringer i dev
            var policies = new List<string>
            {
                "default-src 'self'",
                $"script-src 'self' 'nonce-{nonce}' 'unsafe-inline' 'unsafe-hashes' https://unpkg.com https://cdn.tailwindcss.com",
                $"style-src 'self' 'unsafe-inline' 'unsafe-hashes' https://unpkg.com https://cdn.tailwindcss.com",
                "img-src 'self' data: https://*.tile.openstreetmap.org https://cache.kartverket.no",
                "font-src 'self'",
                "connect-src 'self' ws: wss: http: https:", // Åpner for alt av connect i dev
                "frame-ancestors 'none'",
                "form-action 'self'"
            };

            return string.Join("; ", policies) + ";";
        }
    }

    public static class CspMiddlewareExtensions
    {
        public static IApplicationBuilder UseCspMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CspMiddleware>();
        }
    }
}