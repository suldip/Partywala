namespace PartyClap.Services
{
    public class OtpOptions
    {
        public const string SectionName = "Otp";

        public string Provider { get; set; } = string.Empty;

        /// <summary>When true, OTP is returned to the client (dev/QA fallback).</summary>
        public bool ShowOtpOnScreen { get; set; }

        /// <summary>When SMS fails, still return OTP to the UI if ShowOtpOnScreen is true.</summary>
        public bool ConsoleFallbackOnSmsFailure { get; set; } = true;

        public string SmsUrlTemplate { get; set; } = string.Empty;

        /// <summary>Placeholder OTP value inside SmsUrlTemplate / msg text (e.g. 4444).</summary>
        public string MessagePlaceholder { get; set; } = "4444";

        /// <summary>Sample msisdn in SmsUrlTemplate (e.g. 919891916530).</summary>
        public string SampleMsisdn { get; set; } = "919891916530";

        public int OtpLength { get; set; } = 6;

        public int ExpiryMinutes { get; set; } = 1;

        public int ResendCooldownSeconds { get; set; } = 0;

        public int MaxAttempts { get; set; } = 5;

        /// <summary>Legacy binding for older appsettings keys.</summary>
        public bool ShowDebugCode
        {
            get => ShowOtpOnScreen;
            set => ShowOtpOnScreen = value;
        }

        public bool SmsEnabled =>
            string.Equals(Provider, "SmsIndiaHub", System.StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(SmsUrlTemplate);
    }
}
