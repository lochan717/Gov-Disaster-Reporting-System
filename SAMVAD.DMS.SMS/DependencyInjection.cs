using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SAMVAD.DMS.SMS.Contracts;
using SAMVAD.DMS.SMS.Options;
using SAMVAD.DMS.SMS.Services;

namespace SAMVAD.DMS.SMS;

public static class DependencyInjection
{
    public static IServiceCollection AddSamvadSms(this IServiceCollection services, IConfiguration configuration, string sectionName = "Sms")
    {
        services.Configure<SmsOptions>(configuration.GetSection(sectionName));

        var timeoutSeconds = configuration.GetValue<int?>("Sms:TimeoutSeconds") ?? 30;
        services.AddHttpClient<ISmsSender, HttpGatewaySmsSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds));
        });

        return services;
    }
}