using CareerPlatform.Api.Features.CodeExecution.Service;

namespace CareerPlatform.Api.Features.CodeExecution;

public static class CodeExecutionModule
{
    public static IServiceCollection RegisterCodeExecution(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICodeRunnerService, CodeRunnerService>();
        return services;
    }
}
