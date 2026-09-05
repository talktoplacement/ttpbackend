using CareerPlatform.Api.Features.Meetings.Service;

namespace CareerPlatform.Api.Features.Meetings;

public static class MeetingsModule
{
    public static IServiceCollection RegisterMeetings(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMeetingService, MeetingService>();
        return services;
    }
}
