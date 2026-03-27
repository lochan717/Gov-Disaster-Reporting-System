using Microsoft.Extensions.Logging;
using SAMVAD.DMS.SMS.Contracts;
using SAMVAD.DMS.SMS.Models;
using SAMVAD.DMS.Application.Services;

namespace SAMVAD.DMS.External.Services;

public class SmsService : ISmsService
{
    private readonly ISmsSender _smsSender;
    private readonly ILogger<SmsService> _logger;

    public SmsService(ISmsSender smsSender, ILogger<SmsService> logger)
    {
        _smsSender = smsSender;
        _logger = logger;
    }

    public async Task SendAsync(string mobileNumber, string message, CancellationToken cancellationToken = default)
    {
        await _smsSender.SendAsync(new SmsMessage
        {
            MobileNumber = mobileNumber,
            Message = message
        }, cancellationToken);

        _logger.LogInformation("SMS send request delegated for {MobileNumber}. Message length: {Length}", mobileNumber, message.Length);
    }
}