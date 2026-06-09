using Microsoft.AspNetCore.StaticFiles;

namespace PartyClap.Services
{
    public static class StaticFileCachePolicy
    {
        private static readonly HashSet<string> CacheableExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".css", ".js", ".mjs", ".map", ".woff", ".woff2", ".ttf", ".eot",
            ".svg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".avif"
        };

        public static void Apply(StaticFileResponseContext context)
        {
            var response = context.Context.Response;
            SecurityHeadersMiddleware.StripServerIdentifyingHeaders(response.Headers);
            var fileName = context.File.Name;
            var extension = Path.GetExtension(fileName);

            if (fileName.Equals("sw.js", StringComparison.OrdinalIgnoreCase))
            {
                SetNoCache(response);
                return;
            }

            if (fileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            {
                SetCache(response, TimeSpan.FromDays(1));
                return;
            }

            if (!CacheableExtensions.Contains(extension))
            {
                SetCache(response, TimeSpan.FromHours(1));
                return;
            }

            var hasVersionToken = context.Context.Request.Query.Keys.Any(k =>
                string.Equals(k, "v", StringComparison.OrdinalIgnoreCase));

            SetCache(
                response,
                hasVersionToken ? TimeSpan.FromDays(365) : TimeSpan.FromDays(7),
                immutable: hasVersionToken);
        }

        private static void SetCache(HttpResponse response, TimeSpan maxAge, bool immutable = false)
        {
            var seconds = (int)Math.Max(0, maxAge.TotalSeconds);
            response.Headers.CacheControl = immutable
                ? $"public, max-age={seconds}, immutable"
                : $"public, max-age={seconds}";
            response.Headers.Expires = DateTimeOffset.UtcNow.Add(maxAge).ToString("R");
            response.Headers.Remove("Pragma");
        }

        private static void SetNoCache(HttpResponse response)
        {
            response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            response.Headers.Pragma = "no-cache";
            response.Headers.Expires = "0";
        }
    }
}
