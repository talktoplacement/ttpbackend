using CareerPlatform.Api.Features.StudentProfile.Service;

namespace CareerPlatform.Api.Features.StudentProfile;

public static class StudentProfileModule
{
    public static IServiceCollection RegisterStudentProfile(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IStudentProfileService, StudentProfileService>();
        return services;
    }
}
