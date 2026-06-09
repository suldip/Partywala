using Microsoft.Extensions.Options;

namespace PartyClap.Services
{
    /// <summary>
    /// Stores OTP codes in ASP.NET session with configurable TTL, resend cooldown, and attempt limits.
    /// </summary>
    public class SessionOtpService : IOtpService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionOtpService> _logger;
        private readonly OtpOptions _options;

        public SessionOtpService(
            IHttpContextAccessor httpContextAccessor,
            IOptions<OtpOptions> options,
            ILogger<SessionOtpService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            _logger = logger;
        }

        public string IssueOtp(string key)
        {
            var session = GetSession();
            var otp = GenerateOtp(_options.OtpLength);
            session.SetString(OtpKey(key), otp);
            session.SetString(TimestampKey(key), DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            session.SetString(LastSendKey(key), DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            session.SetString(FailCountKey(key), "0");
            _logger.LogInformation("OTP issued for key ending {KeySuffix}", KeySuffix(key));
            return otp;
        }

        public int GetResendCooldownSecondsRemaining(string key)
        {
            var session = GetSession();
            var lastSendRaw = session.GetString(LastSendKey(key));
            if (string.IsNullOrEmpty(lastSendRaw) || !long.TryParse(lastSendRaw, out var lastSend))
            {
                return 0;
            }

            var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastSend;
            var remaining = _options.ResendCooldownSeconds - (int)elapsed;
            return remaining > 0 ? remaining : 0;
        }

        public bool ValidateOtp(string key, string otp, bool consume = true)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(otp))
            {
                return false;
            }

            var session = GetSession();
            if (GetFailedAttemptCount(session, key) >= _options.MaxAttempts)
            {
                _logger.LogWarning("OTP validation blocked: max attempts exceeded for key ending {KeySuffix}", KeySuffix(key));
                return false;
            }

            var stored = session.GetString(OtpKey(key));
            if (string.IsNullOrEmpty(stored) || stored != otp.Trim())
            {
                IncrementFailedAttempt(session, key);
                return false;
            }

            var timestampRaw = session.GetString(TimestampKey(key));
            if (!string.IsNullOrEmpty(timestampRaw)
                && long.TryParse(timestampRaw, out var issuedAt)
                && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - issuedAt > _options.ExpiryMinutes * 60L)
            {
                ClearOtp(session, key);
                return false;
            }

            if (consume)
            {
                ClearOtp(session, key);
            }

            session.SetString(FailCountKey(key), "0");
            return true;
        }

        public void MarkPhoneVerified(string key)
        {
            GetSession().SetString(VerifiedKey(key), "1");
        }

        public bool IsPhoneVerified(string key)
        {
            return GetSession().GetString(VerifiedKey(key)) == "1";
        }

        public void ClearPhoneVerification(string key)
        {
            GetSession().Remove(VerifiedKey(key));
        }

        private ISession GetSession()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                throw new InvalidOperationException("Session is not available.");
            }

            return session;
        }

        private static string GenerateOtp(int length)
        {
            length = length < 4 ? 6 : length > 8 ? 8 : length;
            var min = (int)Math.Pow(10, length - 1);
            var max = (int)Math.Pow(10, length) - 1;
            return Random.Shared.Next(min, max + 1).ToString();
        }

        private static int GetFailedAttemptCount(ISession session, string key)
        {
            var raw = session.GetString(FailCountKey(key));
            return int.TryParse(raw, out var count) ? count : 0;
        }

        private static void IncrementFailedAttempt(ISession session, string key)
        {
            var count = GetFailedAttemptCount(session, key) + 1;
            session.SetString(FailCountKey(key), count.ToString());
        }

        private static string KeySuffix(string key) => key.Length >= 4 ? key[^4..] : key;

        private static string OtpKey(string key) => "OTP_" + key;
        private static string TimestampKey(string key) => "OTP_TS_" + key;
        private static string VerifiedKey(string key) => "OTP_VERIFIED_" + key;
        private static string LastSendKey(string key) => "OTP_LAST_SEND_" + key;
        private static string FailCountKey(string key) => "OTP_FAIL_" + key;

        private static void ClearOtp(ISession session, string key)
        {
            session.Remove(OtpKey(key));
            session.Remove(TimestampKey(key));
        }
    }
}
