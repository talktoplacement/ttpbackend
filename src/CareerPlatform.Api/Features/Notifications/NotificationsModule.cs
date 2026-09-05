using CareerPlatform.Api.Features.Notifications.Service;

namespace CareerPlatform.Api.Features.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection RegisterNotifications(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}
