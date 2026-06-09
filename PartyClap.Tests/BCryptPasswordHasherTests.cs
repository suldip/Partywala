using PartyClap.Services;
using Xunit;

namespace PartyClap.Tests
{
    public class BCryptPasswordHasherTests
    {
        private readonly IPasswordHasher _hasher = new BCryptPasswordHasher();

        [Fact]
        public void Hash_ProducesBCryptHash_ThatIsNotThePlaintext()
        {
            var hash = _hasher.Hash("Secret123");

            Assert.NotEqual("Secret123", hash);
            Assert.StartsWith("$2", hash);
        }

        [Fact]
        public void Hash_ProducesDifferentHashesForSamePassword_DueToSalt()
        {
            var first = _hasher.Hash("Secret123");
            var second = _hasher.Hash("Secret123");

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Verify_ReturnsTrue_ForCorrectPasswordAgainstHash()
        {
            var hash = _hasher.Hash("Secret123");

            var result = _hasher.Verify("Secret123", hash, out bool needsRehash);

            Assert.True(result);
            Assert.False(needsRehash);
        }

        [Fact]
        public void Verify_ReturnsFalse_ForWrongPassword()
        {
            var hash = _hasher.Hash("Secret123");

            var result = _hasher.Verify("WrongPassword", hash, out _);

            Assert.False(result);
        }

        [Fact]
        public void Verify_AcceptsLegacyPlaintext_AndFlagsForRehash()
        {
            // Existing rows may still hold a plaintext password (pre-hashing).
            var result = _hasher.Verify("admin123", "admin123", out bool needsRehash);

            Assert.True(result);
            Assert.True(needsRehash);
        }

        [Fact]
        public void Verify_RejectsWrongLegacyPlaintext()
        {
            var result = _hasher.Verify("wrong", "admin123", out bool needsRehash);

            Assert.False(result);
            Assert.False(needsRehash);
        }

        [Theory]
        [InlineData(null, "hash")]
        [InlineData("password", null)]
        [InlineData("", "")]
        public void Verify_ReturnsFalse_ForNullOrEmptyInputs(string? password, string? storedHash)
        {
            var result = _hasher.Verify(password, storedHash, out bool needsRehash);

            Assert.False(result);
            Assert.False(needsRehash);
        }
    }
}
