namespace SAMVAD.DMS.SMS.Options;

public sealed class SmsOptions
{
    public bool Enabled { get; set; } = true;
    public string? GatewayUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? SenderId { get; set; }
    public string? Route { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public string MobileFieldName { get; set; } = "mobileNumber";
    public string MessageFieldName { get; set; } = "message";
    public string ApiKeyHeaderName { get; set; } = "X-API-KEY";
}