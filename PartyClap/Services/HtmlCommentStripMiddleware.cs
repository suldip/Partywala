using System.Text;
using System.Text.RegularExpressions;

namespace PartyClap.Services
{
    /// <summary>
    /// Removes HTML comments from rendered pages in production (B031).
    /// </summary>
    public class HtmlCommentStripMiddleware
    {
        private static readonly Regex HtmlCommentRegex = new(@"<!--[\s\S]*?-->", RegexOptions.Compiled);
        private readonly RequestDelegate _next;

        public HtmlCommentStripMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBody = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await _next(context);

                var isHtml = context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;
                if (!isHtml || context.Response.StatusCode != StatusCodes.Status200OK)
                {
                    buffer.Position = 0;
                    context.Response.Body = originalBody;
                    await buffer.CopyToAsync(originalBody);
                    return;
                }

                buffer.Position = 0;
                var html = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
                var stripped = HtmlCommentRegex.Replace(html, string.Empty);
                var bytes = Encoding.UTF8.GetBytes(stripped);

                context.Response.Body = originalBody;
                context.Response.ContentLength = bytes.Length;
                await originalBody.WriteAsync(bytes);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
    }

    public static class HtmlCommentStripMiddlewareExtensions
    {
        public static IApplicationBuilder UseHtmlCommentStripping(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HtmlCommentStripMiddleware>();
        }
    }
}
