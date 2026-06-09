using System.Threading;
using System.Threading.Tasks;

namespace PartyClap.Services
{
    public class NoOpOtpSmsSender : IOtpSmsSender
    {
        public Task<bool> SendOtpAsync(string mobile, string otp, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
