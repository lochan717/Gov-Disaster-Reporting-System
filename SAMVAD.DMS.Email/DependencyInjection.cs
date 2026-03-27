using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SAMVAD.DMS.Email.Contracts;
using SAMVAD.DMS.Email.Options;
using SAMVAD.DMS.Email.Services;

namespace SAMVAD.DMS.Email;

public static class DependencyInjection
{
    public static IServiceCollection AddSamvadEmail(this IServiceCollection services, IConfiguration configuration, string sectionName = "Email")
    {
        services.Configure<EmailOptions>(configuration.GetSection(sectionName));
        services.AddSingleton<IEmailTemplateRenderer, EmbeddedEmailTemplateRenderer>();
        services.AddScoped<IEmailSender, MailKitEmailSender>();

        return services;
    }
}