using Microsoft.Extensions.Logging;
using SAMVAD.DMS.Email.Contracts;
using SAMVAD.DMS.Email.Models;
using SAMVAD.DMS.Application.Services;

namespace SAMVAD.DMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IEmailSender emailSender, IEmailTemplateRenderer templateRenderer, ILogger<EmailService> logger)
    {
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        await _emailSender.SendAsync(new EmailMessage
        {
            ToEmail = toEmail,
            Subject = subject,
            HtmlBody = htmlBody
        }, cancellationToken);

        _logger.LogInformation("Email send request delegated for {Email} with subject {Subject}", toEmail, subject);
    }

    public async Task SendTemplatedAsync(
        string toEmail,
        string subject,
        string templateName,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default)
    {
        var htmlBody = _templateRenderer.Render(templateName, tokens);
        await SendAsync(toEmail, subject, htmlBody, cancellationToken);
    }
}