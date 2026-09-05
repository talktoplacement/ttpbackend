using CareerPlatform.Api.Features.Courses.Service;

namespace CareerPlatform.Api.Features.Courses;

/// <summary>
/// DI registration for the Courses feature module. Program.cs calls
/// <c>builder.Services.RegisterCourses(builder.Configuration)</c> once; every feature-owned
/// abstraction gets wired here. Additional wiring (options, event handlers, background jobs)
/// belongs on this method too so the feature is self-contained.
/// </summary>
public static class CoursesModule
{
    public static IServiceCollection RegisterCourses(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IStudentCourseService, StudentCourseService>();
        return services;
    }
}
