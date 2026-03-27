namespace SAMVAD.DMS.Application.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendTemplatedAsync(
        string toEmail,
        string subject,
        string templateName,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default);
}