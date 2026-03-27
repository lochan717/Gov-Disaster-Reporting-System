using Microsoft.Extensions.DependencyInjection;
using SAMVAD.DMS.Application.Services;

namespace SAMVAD.DMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IDistrictService, DistrictService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}