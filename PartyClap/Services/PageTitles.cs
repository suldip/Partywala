using System.Text.RegularExpressions;

namespace PartyClap.Services
{
    public static class PageTitles
    {
        public const string SiteName = "PartyClap";

        public static string Resolve(string? title, string? controller, string? action)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title.Trim();
            }

            return (controller, action) switch
            {
                ("Home", "Index") => "Book Party Vendors & Event Services",
                ("Home", "Contact") => "Contact Us",
                ("Home", "Privacy") => "Privacy Policy",
                ("Home", "TrustCenter") => "Trust Center & Dispute Resolution",
                ("Home", "Error") => "Something Went Wrong",
                ("Home", "PageNotFound") => "Page Not Found",
                ("Customer", "Explore") => "Explore Party Services",
                ("Customer", "Dashboard") => "My Bookings & Dashboard",
                ("Customer", "Cart") => "Your Cart",
                ("Customer", "Register") => "Create Customer Account",
                ("Account", "Login") => "Login",
                ("Account", "Signup") => "Sign Up",
                ("Vendor", "Dashboard") => "Vendor Dashboard",
                ("Vendor", "Register") => "Vendor Registration",
                ("Admin", "Dashboard") => "Admin Dashboard",
                _ => ResolveFromRoute(controller, action)
            };
        }

        public static string FormatBrowserTitle(string pageTitle)
        {
            if (string.IsNullOrWhiteSpace(pageTitle) || pageTitle == SiteName)
            {
                return SiteName;
            }

            return $"{pageTitle} | {SiteName}";
        }

        private static string ResolveFromRoute(string? controller, string? action)
        {
            var humanizedAction = HumanizeAction(action);
            if (!string.IsNullOrWhiteSpace(humanizedAction))
            {
                return humanizedAction;
            }

            return HumanizeAction(controller) ?? SiteName;
        }

        private static string? HumanizeAction(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "Index")
            {
                return null;
            }

            return Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
        }
    }
}
