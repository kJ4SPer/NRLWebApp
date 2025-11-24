using Microsoft.AspNetCore.Mvc.Rendering;

namespace FirstWebApplication.Helpers
{
    public static class CspHelper
    {
        /// <summary>
        /// Gets the CSP nonce for the current request
        /// </summary>
        public static string GetNonce(this IHtmlHelper htmlHelper)
        {
            var httpContext = htmlHelper.ViewContext.HttpContext;
            if (httpContext.Items.TryGetValue("csp-nonce", out var nonce))
            {
                return nonce?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Generates a nonce attribute for script/style tags
        /// </summary>
        public static string NonceAttribute(this IHtmlHelper htmlHelper)
        {
            var nonce = htmlHelper.GetNonce();
            return string.IsNullOrEmpty(nonce) ? string.Empty : $"nonce=\"{nonce}\"";
        }
    }
}
