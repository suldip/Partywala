using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PartyClap.Services
{
    public class SmsIndiaHubOtpSender : IOtpSmsSender
    {
        private readonly HttpClient _httpClient;
        private readonly OtpOptions _options;
        private readonly ILogger<SmsIndiaHubOtpSender> _logger;

        public SmsIndiaHubOtpSender(
            HttpClient httpClient,
            IOptions<OtpOptions> options,
            ILogger<SmsIndiaHubOtpSender> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> SendOtpAsync(string mobile, string otp, CancellationToken cancellationToken = default)
        {
            if (!_options.SmsEnabled)
            {
                return false;
            }

            var phoneError = PhoneRules.ValidateIndianMobile(mobile, out var normalizedMobile);
            if (phoneError != null)
            {
                _logger.LogWarning("SMS India Hub skipped: invalid mobile.");
                return false;
            }

            var msisdn = "91" + normalizedMobile;
            var url = BuildRequestUrl(msisdn, otp);
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("SMS India Hub skipped: URL template is empty.");
                return false;
            }

            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "SMS India Hub HTTP {Status} for mobile ending {Suffix}. Body: {Body}",
                        (int)response.StatusCode,
                        normalizedMobile.Length >= 4 ? normalizedMobile[^4..] : normalizedMobile,
                        Truncate(body, 300));
                    return false;
                }

                _logger.LogInformation(
                    "SMS India Hub OTP dispatched for mobile ending {Suffix}. Response: {Body}",
                    normalizedMobile.Length >= 4 ? normalizedMobile[^4..] : normalizedMobile,
                    Truncate(body, 300));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SMS India Hub request failed for mobile ending {Suffix}",
                    normalizedMobile.Length >= 4 ? normalizedMobile[^4..] : normalizedMobile);
                return false;
            }
        }

        private string BuildRequestUrl(string msisdn, string otp)
        {
            var template = _options.SmsUrlTemplate?.Trim();
            if (string.IsNullOrEmpty(template))
            {
                return null;
            }

            var placeholder = string.IsNullOrWhiteSpace(_options.MessagePlaceholder)
                ? "4444"
                : _options.MessagePlaceholder.Trim();

            var sampleMsisdn = string.IsNullOrWhiteSpace(_options.SampleMsisdn)
                ? "919891916530"
                : _options.SampleMsisdn.Trim();

            var url = template
                .Replace(sampleMsisdn, msisdn, StringComparison.OrdinalIgnoreCase)
                .Replace(placeholder, otp, StringComparison.Ordinal);

            return url;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }
    }
}
