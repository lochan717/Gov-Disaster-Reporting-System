using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SAMVAD.DMS.Email.Contracts;
using SAMVAD.DMS.Email.Models;
using SAMVAD.DMS.Email.Options;

namespace SAMVAD.DMS.Email.Services;

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly IOptionsMonitor<EmailOptions> _optionsMonitor;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptionsMonitor<EmailOptions> optionsMonitor, ILogger<MailKitEmailSender> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skipping recipient {Email}", message.ToEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost is required when email sending is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException("Email:FromAddress is required when email sending is enabled.");
        }

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(options.FromName ?? options.FromAddress, options.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.ToEmail));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = string.IsNullOrWhiteSpace(message.TextBody) ? null : message.TextBody
        }.ToMessageBody();

        using var smtpClient = new SmtpClient();

        var socketOptions = ResolveSocketOptions(options);
        await smtpClient.ConnectAsync(options.SmtpHost, options.SmtpPort, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.SmtpUsername))
        {
            await smtpClient.AuthenticateAsync(options.SmtpUsername, options.SmtpPassword, cancellationToken);
        }

        await smtpClient.SendAsync(mimeMessage, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Email sent to {Email} with subject {Subject}", message.ToEmail, message.Subject);
    }

    private static SecureSocketOptions ResolveSocketOptions(EmailOptions options)
    {
        if (options.UseSsl)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        if (options.UseStartTls)
        {
            return SecureSocketOptions.StartTls;
        }

        return SecureSocketOptions.None;
    }
}