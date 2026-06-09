using System.Linq;
using System.Text.RegularExpressions;

namespace PartyClap.Services
{
    public static class BankDetailsRules
    {
        public const int AccountHolderMinLength = 2;
        public const int AccountHolderMaxLength = 100;
        public const int AccountNumberMinDigits = 9;
        public const int AccountNumberMaxDigits = 18;

        private static readonly Regex AccountHolderRegex = new(@"^[A-Za-z\s.'-]+$", RegexOptions.Compiled);
        private static readonly Regex AccountNumberRegex = new(@"^\d{9,18}$", RegexOptions.Compiled);
        private static readonly Regex IfscRegex = new(@"^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.Compiled);
        private static readonly Regex UpiRegex = new(@"^[A-Za-z0-9._-]{2,256}@[A-Za-z]{2,64}$", RegexOptions.Compiled);

        public static string ValidateAccountHolderName(string value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return "Account holder name is required.";
            }

            if (value.Length < AccountHolderMinLength || value.Length > AccountHolderMaxLength)
            {
                return $"Account holder name must be between {AccountHolderMinLength} and {AccountHolderMaxLength} characters.";
            }

            if (!AccountHolderRegex.IsMatch(value))
            {
                return "Account holder name may only contain letters, spaces, periods, hyphens, and apostrophes.";
            }

            return null;
        }

        public static string ValidateAccountNumber(string value)
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits))
            {
                return "Account number is required.";
            }

            if (!AccountNumberRegex.IsMatch(digits))
            {
                return $"Account number must be {AccountNumberMinDigits} to {AccountNumberMaxDigits} digits.";
            }

            return null;
        }

        public static string NormalizeIfscCode(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
        }

        public static string ValidateIfscCode(string value, out string normalized)
        {
            normalized = NormalizeIfscCode(value);
            if (string.IsNullOrEmpty(normalized))
            {
                return "IFSC code is required.";
            }

            if (normalized.Length != 11 || !IfscRegex.IsMatch(normalized))
            {
                return "Enter a valid 11-character IFSC code (e.g., HDFC0001234).";
            }

            return null;
        }

        public static string ValidateUpiId(string value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return "UPI ID is required.";
            }

            if (value.Length > 256 || !UpiRegex.IsMatch(value))
            {
                return "Enter a valid UPI ID (e.g., name@bank).";
            }

            return null;
        }

        /// <summary>
        /// Validates payout fields when saving vendor profile. All empty is allowed.
        /// </summary>
        public static string ValidatePayoutDetails(string holder, string account, string ifsc, string upi)
        {
            holder = holder?.Trim();
            account = new string((account ?? string.Empty).Where(char.IsDigit).ToArray());
            ifsc = NormalizeIfscCode(ifsc);
            upi = upi?.Trim();

            var hasBank = !string.IsNullOrEmpty(holder)
                || !string.IsNullOrEmpty(account)
                || !string.IsNullOrEmpty(ifsc);
            var hasUpi = !string.IsNullOrEmpty(upi);

            if (!hasBank && !hasUpi)
            {
                return null;
            }

            if (hasUpi)
            {
                var upiError = ValidateUpiId(upi);
                if (upiError != null)
                {
                    return upiError;
                }
            }

            if (hasBank)
            {
                var holderError = ValidateAccountHolderName(holder);
                if (holderError != null)
                {
                    return holderError;
                }

                var accountError = ValidateAccountNumber(account);
                if (accountError != null)
                {
                    return accountError;
                }

                var ifscError = ValidateIfscCode(ifsc, out _);
                if (ifscError != null)
                {
                    return ifscError;
                }
            }

            return null;
        }
    }
}
