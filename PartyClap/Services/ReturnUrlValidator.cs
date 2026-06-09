using Microsoft.AspNetCore.Mvc;

namespace PartyClap.Services
{
    public static class ReturnUrlValidator
    {
        /// <summary>
        /// Returns the URL when it is a safe same-site relative path; otherwise null.
        /// </summary>
        public static string GetSafeLocalUrl(IUrlHelper urlHelper, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return null;
            }

            var trimmed = returnUrl.Trim();

            if (trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.Contains("://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("\\", StringComparison.Ordinal)
                || trimmed.Contains('\\')
                || trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!urlHelper.IsLocalUrl(trimmed))
            {
                return null;
            }

            return trimmed;
        }

        public static bool IsSafeLocalUrl(IUrlHelper urlHelper, string? returnUrl)
        {
            return GetSafeLocalUrl(urlHelper, returnUrl) != null;
        }
    }
}
