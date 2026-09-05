using CareerPlatform.Api.Features.Dashboard.Dto;

namespace CareerPlatform.Api.Features.Dashboard.Service;

public interface IDashboardService
{
    Task<Result<AdminStatsResponse>> GetAdminStatsAsync(string filter, CancellationToken ct);
    Task<Result<IReadOnlyList<RegisteredStudentResponse>>> GetRegisteredStudentsAsync(CancellationToken ct);
    Task<Result<RegisteredStudentResponse>> GetStudentByIdAsync(string id, CancellationToken ct);
    Task<Result<IReadOnlyList<StudentPerformanceResponse>>> GetStudentPerformanceAsync(CancellationToken ct);
    Task<Result<AnalyticsOverviewResponse>> GetAnalyticsOverviewAsync(int months, CancellationToken ct);
}
