using System.Threading;
using System.Threading.Tasks;

namespace PartyClap.Services
{
    public interface IOtpSmsSender
    {
        Task<bool> SendOtpAsync(string mobile, string otp, CancellationToken cancellationToken = default);
    }
}
