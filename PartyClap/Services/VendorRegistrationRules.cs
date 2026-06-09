using System.Linq;
using System.Text.RegularExpressions;

namespace PartyClap.Services
{
    public static class VendorRegistrationRules
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 150;
        public const int AddressMinLength = 5;
        public const int AddressMaxLength = 500;

        private static readonly Regex NameRegex = new(@"^[A-Za-z\s.'-]+$", RegexOptions.Compiled);
        private static readonly Regex PinCodeRegex = new(@"^[1-9][0-9]{5}$", RegexOptions.Compiled);

        public static string ValidateName(string name)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return "Name is required.";
            }

            if (name.Length < NameMinLength || name.Length > NameMaxLength)
            {
                return $"Name must be between {NameMinLength} and {NameMaxLength} characters.";
            }

            if (!NameRegex.IsMatch(name))
            {
                return "Name may only contain letters, spaces, periods, hyphens, and apostrophes.";
            }

            return null;
        }

        public static string NormalizePinCode(string pinCode)
        {
            if (string.IsNullOrWhiteSpace(pinCode))
            {
                return null;
            }

            var digits = new string(pinCode.Where(char.IsDigit).ToArray());
            return digits.Length == 0 ? null : digits;
        }

        public static string ValidatePinCode(string pinCode, out string normalized)
        {
            normalized = NormalizePinCode(pinCode);
            if (string.IsNullOrEmpty(normalized))
            {
                return "Pin code is required.";
            }

            if (!PinCodeRegex.IsMatch(normalized))
            {
                return "Enter a valid 6-digit Indian pin code.";
            }

            return null;
        }

        public static string ValidateAddress(string address)
        {
            address = address?.Trim();
            if (string.IsNullOrEmpty(address))
            {
                return "Street address is required.";
            }

            if (address.Length < AddressMinLength || address.Length > AddressMaxLength)
            {
                return $"Street address must be between {AddressMinLength} and {AddressMaxLength} characters.";
            }

            return null;
        }
    }

}
