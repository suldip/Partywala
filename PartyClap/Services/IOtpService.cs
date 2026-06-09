namespace PartyClap.Services
{
    public interface IOtpService
    {
        /// <summary>Generates an OTP, stores it in session, and returns the code.</summary>
        string IssueOtp(string key);

        /// <summary>Seconds remaining before another OTP can be sent, or 0 when allowed.</summary>
        int GetResendCooldownSecondsRemaining(string key);

        /// <summary>Returns true when the OTP matches and has not expired.</summary>
        bool ValidateOtp(string key, string otp, bool consume = true);

        void MarkPhoneVerified(string key);

        bool IsPhoneVerified(string key);

        void ClearPhoneVerification(string key);
    }
}
