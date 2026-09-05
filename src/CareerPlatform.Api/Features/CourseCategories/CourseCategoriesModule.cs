using CareerPlatform.Api.Features.CourseCategories.Service;

namespace CareerPlatform.Api.Features.CourseCategories;

public static class CourseCategoriesModule
{
    public static IServiceCollection RegisterCourseCategories(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICourseCategoryService, CourseCategoryService>();
        return services;
    }
}
