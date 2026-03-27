using System.Net;
using System.Reflection;
using SAMVAD.DMS.Email.Contracts;

namespace SAMVAD.DMS.Email.Services;

public sealed class EmbeddedEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly Assembly = typeof(EmbeddedEmailTemplateRenderer).Assembly;
    private const string ResourcePrefix = "SAMVAD.DMS.Email.Templates.";

    public string Render(string templateName, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Template name is required.", nameof(templateName));
        }

        var normalizedTemplateName = NormalizeTemplateName(templateName);
        var resourceName = $"{ResourcePrefix}{normalizedTemplateName}.html";

        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Email template '{templateName}' was not found.");
        using var reader = new StreamReader(stream);

        var html = reader.ReadToEnd();
        var mergeTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
        };

        foreach (var kvp in tokens)
        {
            mergeTokens[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in mergeTokens)
        {
            html = html.Replace($"{{{{{kvp.Key}}}}}", WebUtility.HtmlEncode(kvp.Value ?? string.Empty), StringComparison.OrdinalIgnoreCase);
        }

        return html;
    }

    private static string NormalizeTemplateName(string templateName)
    {
        return templateName
            .Trim()
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}