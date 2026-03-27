using SAMVAD.DMS.Email.Models;

namespace SAMVAD.DMS.Email.Contracts;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}