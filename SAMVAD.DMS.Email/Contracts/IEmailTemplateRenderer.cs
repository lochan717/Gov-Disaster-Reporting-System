namespace SAMVAD.DMS.Email.Contracts;

public interface IEmailTemplateRenderer
{
    string Render(string templateName, IReadOnlyDictionary<string, string> tokens);
}