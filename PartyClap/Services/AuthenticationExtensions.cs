using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace PartyClap.Services
{
    /// <summary>
    /// Centralizes sign-in/sign-out so identity is represented by a signed
    /// authentication cookie (claims), replacing the previous ad-hoc, forgeable
    /// session-string role checks. Session values are still written for backward
    /// compatibility with views/JS that read them.
    /// </summary>
    public static class AuthenticationExtensions
    {
        public static async Task SignInUserAsync(this HttpContext httpContext, string role, string userId, string userName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = true });

            // Backward-compatible session mirror (cart, notifications and legacy reads).
            httpContext.Session.SetString("UserRole", role);
            httpContext.Session.SetString("UserId", userId);
            httpContext.Session.SetString("UserName", userName ?? string.Empty);
            httpContext.Session.SetString("UserType", role);
        }

        public static async Task SignOutUserAsync(this HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            httpContext.Session.Clear();
        }

        public static string GetUserId(this HttpContext httpContext)
        {
            return httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? httpContext.Session.GetString("UserId");
        }

        public static string GetUserRole(this HttpContext httpContext)
        {
            return httpContext.User?.FindFirst(ClaimTypes.Role)?.Value
                   ?? httpContext.Session.GetString("UserRole");
        }
    }
}
