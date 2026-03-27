using SAMVAD.DMS.SMS.Models;

namespace SAMVAD.DMS.SMS.Contracts;

public interface ISmsSender
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}