namespace SAMVAD.DMS.SMS.Models;

public sealed class SmsMessage
{
    public required string MobileNumber { get; init; }
    public required string Message { get; init; }
}