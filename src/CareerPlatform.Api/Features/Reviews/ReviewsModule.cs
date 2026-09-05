using CareerPlatform.Api.Features.Reviews.Service;

namespace CareerPlatform.Api.Features.Reviews;

public static class ReviewsModule
{
    public static IServiceCollection RegisterReviews(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReviewService, ReviewService>();
        return services;
    }
}
