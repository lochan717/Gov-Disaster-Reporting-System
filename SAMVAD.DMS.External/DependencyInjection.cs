using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SAMVAD.DMS.Application.Services;
using SAMVAD.DMS.SMS;
using SAMVAD.DMS.External.Services;

namespace SAMVAD.DMS.External;

public static class DependencyInjection
{
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSamvadSms(configuration);
        services.AddScoped<ISmsService, SmsService>();
        return services;
    }
}