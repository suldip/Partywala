using System;
using System.Collections.Generic;
using System.Linq;

namespace PartyClap.Services
{
    /// <summary>
    /// Shared email validation for registration and profile updates.
    /// Blocks known test/placeholder addresses and cross-role duplicates.
    /// </summary>
    public static class EmailRules
    {
        private static readonly HashSet<string> BlockedAddresses = new(StringComparer.OrdinalIgnoreCase)
        {
            "customer@test.com",
            "test@test.com",
            "admin@test.com",
            "vendor@test.com",
            "user@example.com",
            "test@example.com"
        };

        private static readonly string[] BlockedDomains =
        {
            "example.com",
            "example.org",
            "example.net",
            "test.com",
            "localhost"
        };

        public static string Normalize(string email) => email?.Trim();

        public static bool IsBlockedTestEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;

            email = Normalize(email);
            if (BlockedAddresses.Contains(email)) return true;

            var at = email.LastIndexOf('@');
            if (at < 0) return true;

            var domain = email.Substring(at + 1);
            if (BlockedDomains.Any(d => domain.Equals(d, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var local = email.Substring(0, at);
            return local.Equals("test", StringComparison.OrdinalIgnoreCase)
                || local.Equals("placeholder", StringComparison.OrdinalIgnoreCase)
                || local.Equals("dummy", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns an error message when invalid, or null when the email may be used.
        /// </summary>
        public static string ValidateVendorEmail(IDataService dataService, string email, string currentVendorId = null, string verifiedPhone = null)
        {
            email = Normalize(email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Email is required.";
            }

            if (IsBlockedTestEmail(email))
            {
                return "Please use your real business email. Test or placeholder emails are not allowed.";
            }

            var customer = dataService.GetCustomerByEmail(email);
            if (customer != null)
            {
                var ownerPhone = PhoneRules.NormalizeIndianMobile(verifiedPhone);
                var customerPhone = PhoneRules.NormalizeIndianMobile(customer.Phone);
                var isSamePerson = !string.IsNullOrEmpty(ownerPhone)
                    && !string.IsNullOrEmpty(customerPhone)
                    && string.Equals(ownerPhone, customerPhone, StringComparison.Ordinal);

                if (!isSamePerson)
                {
                    return "This email belongs to a customer account. Use a different email for your vendor profile.";
                }
            }

            var otherVendor = dataService.GetVendorByEmail(email);
            if (otherVendor != null && !string.Equals(otherVendor.Id, currentVendorId, StringComparison.Ordinal))
            {
                return "Another vendor account already uses this email.";
            }

            return null;
        }
    }
}
