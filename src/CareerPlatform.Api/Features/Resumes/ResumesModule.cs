using CareerPlatform.Api.Features.Resumes.Service;

namespace CareerPlatform.Api.Features.Resumes;

public static class ResumesModule
{
    public static IServiceCollection RegisterResumes(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IResumesService, ResumesService>();
        services.AddScoped<IResumeDraftService, ResumeDraftService>();
        // ATS scoring is pure + stateless — singletons are safe and avoid per-request allocation.
        services.AddSingleton<IResumeTextExtractor, PdfResumeTextExtractor>();
        services.AddSingleton<IResumeAtsAnalyzer, ResumeAtsAnalyzer>();
        return services;
    }
}
