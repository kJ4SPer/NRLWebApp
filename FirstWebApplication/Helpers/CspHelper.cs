using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;

namespace FirstWebApplication.Helpers
{
    public static class CspHelper
    {
        public static string GetNonce(this IHtmlHelper htmlHelper)
        {
            var httpContext = htmlHelper.ViewContext.HttpContext;

            // Prøv å hente nonce fra context (satt av middleware)
            if (httpContext.Items.TryGetValue("csp-nonce", out var nonce) && nonce != null)
            {
                return nonce.ToString() ?? "";
            }

            // FALLBACK: Hvis middleware ikke kjørte, lag en ny her og nå.
            // Dette hindrer "Object reference not set" feil.
            var newNonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            httpContext.Items["csp-nonce"] = newNonce;

            return newNonce;
        }

        public static string NonceAttribute(this IHtmlHelper htmlHelper)
        {
            var nonce = htmlHelper.GetNonce();
            return string.IsNullOrEmpty(nonce) ? string.Empty : $"nonce=\"{nonce}\"";
        }
    }
}