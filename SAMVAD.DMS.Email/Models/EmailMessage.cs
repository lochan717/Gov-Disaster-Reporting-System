namespace SAMVAD.DMS.Email.Models;

public sealed class EmailMessage
{
    public required string ToEmail { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
}