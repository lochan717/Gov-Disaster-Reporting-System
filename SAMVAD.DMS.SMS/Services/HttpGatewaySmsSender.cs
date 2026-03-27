using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAMVAD.DMS.SMS.Contracts;
using SAMVAD.DMS.SMS.Models;
using SAMVAD.DMS.SMS.Options;

namespace SAMVAD.DMS.SMS.Services;

public sealed class HttpGatewaySmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SmsOptions> _optionsMonitor;
    private readonly ILogger<HttpGatewaySmsSender> _logger;

    public HttpGatewaySmsSender(HttpClient httpClient, IOptionsMonitor<SmsOptions> optionsMonitor, ILogger<HttpGatewaySmsSender> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            _logger.LogInformation("SMS sending is disabled. Skipping recipient {Mobile}", message.MobileNumber);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.GatewayUrl))
        {
            throw new InvalidOperationException("Sms:GatewayUrl is required when sms sending is enabled.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.GatewayUrl);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
        }

        var payload = new Dictionary<string, object?>
        {
            [options.MobileFieldName] = message.MobileNumber,
            [options.MessageFieldName] = message.Message
        };

        if (!string.IsNullOrWhiteSpace(options.SenderId))
        {
            payload["senderId"] = options.SenderId;
        }

        if (!string.IsNullOrWhiteSpace(options.Route))
        {
            payload["route"] = options.Route;
        }

        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("SMS sent to {Mobile}", message.MobileNumber);
            return;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"SMS gateway request failed with status {(int)response.StatusCode}: {error}");
    }
}