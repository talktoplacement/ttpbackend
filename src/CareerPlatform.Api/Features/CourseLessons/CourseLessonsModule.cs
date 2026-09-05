using CareerPlatform.Api.Features.CourseLessons.Service;

namespace CareerPlatform.Api.Features.CourseLessons;

public static class CourseLessonsModule
{
    public static IServiceCollection RegisterCourseLessons(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICourseLessonService, CourseLessonService>();
        return services;
    }
}
