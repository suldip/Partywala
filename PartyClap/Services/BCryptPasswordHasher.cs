using System;

namespace PartyClap.Services
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;

        public string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password must not be empty.", nameof(password));
            }
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool Verify(string password, string storedHash, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            // Existing rows may still hold legacy plaintext passwords (pre-hashing).
            // Detect a BCrypt hash by its prefix; otherwise fall back to a constant-ish
            // plaintext comparison and flag the credential for re-hashing on success.
            if (IsBCryptHash(storedHash))
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }

            bool matches = string.Equals(password, storedHash, StringComparison.Ordinal);
            needsRehash = matches;
            return matches;
        }

        private static bool IsBCryptHash(string value)
        {
            return value.Length >= 4 &&
                   value[0] == '$' &&
                   (value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$"));
        }
    }
}
