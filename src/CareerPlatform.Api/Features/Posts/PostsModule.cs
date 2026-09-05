using CareerPlatform.Api.Features.Posts.Service;

namespace CareerPlatform.Api.Features.Posts;

public static class PostsModule
{
    public static IServiceCollection RegisterPosts(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPostService, PostService>();
        return services;
    }
}
