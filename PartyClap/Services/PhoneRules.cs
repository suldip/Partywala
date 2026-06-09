using System.Linq;
using System.Text.RegularExpressions;

namespace PartyClap.Services
{
    public static class PhoneRules
    {
        private static readonly Regex IndianMobileRegex = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);

        public static bool IsAdminEmail(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains('@');
        }

        /// <summary>Strips formatting and optional +91 / leading 0 prefixes.</summary>
        public static string NormalizeIndianMobile(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length == 12 && digits.StartsWith("91"))
            {
                digits = digits[2..];
            }
            else if (digits.Length == 11 && digits.StartsWith("0"))
            {
                digits = digits[1..];
            }

            return digits.Length == 0 ? null : digits;
        }

        /// <summary>Returns an error message when invalid, or null when acceptable.</summary>
        public static string ValidateIndianMobile(string value, out string normalized)
        {
            normalized = NormalizeIndianMobile(value);
            if (string.IsNullOrEmpty(normalized))
            {
                return "Enter a valid 10-digit Indian mobile number.";
            }

            if (normalized.Length != 10)
            {
                return "Mobile number must be exactly 10 digits.";
            }

            if (!IndianMobileRegex.IsMatch(normalized))
            {
                return "Enter a valid Indian mobile number starting with 6, 7, 8, or 9.";
            }

            return null;
        }
    }
}
