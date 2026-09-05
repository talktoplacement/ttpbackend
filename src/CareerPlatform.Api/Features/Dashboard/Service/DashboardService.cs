using CareerPlatform.Api.Features.Dashboard.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Dashboard.Service;

/// <summary>Admin dashboard aggregator. Ports three MediatR handlers verbatim.</summary>
internal sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly Func<DateTime> _utcNow;

    public DashboardService(AppDbContext db, Func<DateTime>? utcNow = null)
    {
        _db = db;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public static DateTime ResolveStartDate(string filter, DateTime now) =>
        (filter ?? string.Empty).ToLower() switch
        {
            "today" => now.Date,
            "week" => now.AddDays(-7),
            "month" => now.AddMonths(-1),
            "year" => now.AddYears(-1),
            _ => now.AddMonths(-1)
        };

    public async Task<Result<AdminStatsResponse>> GetAdminStatsAsync(string filter, CancellationToken ct)
    {
        var now = _utcNow();
        var startDate = ResolveStartDate(filter, now);

        var totalStudents = await _db.UserProfiles.CountAsync(u => u.Role == "Student", ct);
        var freeUsers = await _db.UserProfiles.CountAsync(u => u.Role == "Student" && u.PlanName == "Free", ct);
        var monthlyUsers = await _db.UserProfiles.CountAsync(u => u.Role == "Student" && u.PlanName == "Monthly (Pro)", ct);
        var yearlyUsers = await _db.UserProfiles.CountAsync(u => u.Role == "Student" && u.PlanName == "Yearly (Premium)", ct);

        var txQuery = _db.Transactions.Where(t => t.Date >= startDate && t.Date <= now);
        var totalAmount = await txQuery.SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        var count = await txQuery.CountAsync(ct);

        return Result.Success(new AdminStatsResponse(
            totalStudents, freeUsers, monthlyUsers, yearlyUsers,
            new RevenueResponse(totalAmount, count)));
    }

    public async Task<Result<IReadOnlyList<RegisteredStudentResponse>>> GetRegisteredStudentsAsync(CancellationToken ct)
    {
        var students = await (from u in _db.UserProfiles
                              where u.Role == "Student"
                              let latestOrder = _db.Orders
                                  .Where(o => o.UserId == u.Id && o.Status == "Paid")
                                  .OrderByDescending(o => o.CreatedAt)
                                  .FirstOrDefault()
                              orderby u.CreatedAt descending
                              select new RegisteredStudentResponse(
                                  string.IsNullOrEmpty(u.FullName) ? "Student" : u.FullName,
                                  u.Email,
                                  string.IsNullOrEmpty(u.Phone) ? "—" : u.Phone,
                                  string.IsNullOrEmpty(u.PlanName) ? "Free Tier" : u.PlanName,
                                  u.CreatedAt.ToString("dd MMM yyyy"),
                                  latestOrder != null ? latestOrder.CreatedAt.ToString("dd MMM yyyy") : "—",
                                  "Active",
                                  string.IsNullOrEmpty(u.FullName) ? "Student" : u.FullName,
                                  u.Email,
                                  string.IsNullOrEmpty(u.PlanName) ? "Free Tier" : u.PlanName,
                                  u.CreatedAt.ToString("dd MMM yyyy"),
                                  u.Id
                              )).ToListAsync(ct);
        return Result.Success<IReadOnlyList<RegisteredStudentResponse>>(students);
    }

    public async Task<Result<RegisteredStudentResponse>> GetStudentByIdAsync(string id, CancellationToken ct)
    {
        var u = await _db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Role == "Student", ct);
        if (u is null)
        {
            return Result.Failure<RegisteredStudentResponse>(Error.NotFound(
                "Student.NotFound", $"Student '{id}' was not found."));
        }
        var latestOrder = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == u.Id && o.Status == "Paid")
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);
        var name = string.IsNullOrEmpty(u.FullName) ? "Student" : u.FullName;
        var plan = string.IsNullOrEmpty(u.PlanName) ? "Free Tier" : u.PlanName;
        var response = new RegisteredStudentResponse(
            name,
            u.Email,
            string.IsNullOrEmpty(u.Phone) ? "—" : u.Phone,
            plan,
            u.CreatedAt.ToString("dd MMM yyyy"),
            latestOrder != null ? latestOrder.CreatedAt.ToString("dd MMM yyyy") : "—",
            "Active",
            name,
            u.Email,
            plan,
            u.CreatedAt.ToString("dd MMM yyyy"),
            u.Id);
        return Result.Success(response);
    }

    public async Task<Result<AnalyticsOverviewResponse>> GetAnalyticsOverviewAsync(int months, CancellationToken ct)
    {
        // Clamp the window so a caller can't request an unbounded scan.
        var window = Math.Clamp(months, 1, 24);
        var now = _utcNow();
        // First day (UTC) of the earliest month in the window.
        var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(window - 1));

        var revByMonth = await _db.Transactions
            .Where(t => t.Date >= windowStart)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var enrByMonth = await _db.UserProfiles
            .Where(u => u.Role == "Student" && u.CreatedAt >= windowStart)
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var revenueSeries = new List<AnalyticsPoint>(window);
        var enrollmentSeries = new List<AnalyticsPoint>(window);
        for (var i = 0; i < window; i++)
        {
            var month = windowStart.AddMonths(i);
            var label = month.ToString("MMM");
            var rev = revByMonth.FirstOrDefault(r => r.Year == month.Year && r.Month == month.Month);
            var enr = enrByMonth.FirstOrDefault(e => e.Year == month.Year && e.Month == month.Month);
            revenueSeries.Add(new AnalyticsPoint(label, rev?.Total ?? 0m));
            enrollmentSeries.Add(new AnalyticsPoint(label, enr?.Count ?? 0));
        }

        var totalRevenue = revenueSeries.Sum(p => p.Value);
        var totalStudents = await _db.UserProfiles.CountAsync(u => u.Role == "Student", ct);
        var paidStudents = await _db.UserProfiles.CountAsync(
            u => u.Role == "Student" && u.PlanName != null && u.PlanName != "" && u.PlanName != "Free", ct);

        return Result.Success(new AnalyticsOverviewResponse(
            totalRevenue, totalStudents, paidStudents, revenueSeries, enrollmentSeries));
    }

    public async Task<Result<IReadOnlyList<StudentPerformanceResponse>>> GetStudentPerformanceAsync(CancellationToken ct)
    {
        var thirtyDaysAgo = _utcNow().AddDays(-30);
        var grouped = await _db.ProgressLogs
            .Where(p => p.LogDate >= thirtyDaysAgo)
            .GroupBy(p => p.LogDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                WatchSeconds = g.Sum(p => p.WatchDurationSeconds),
                CompletedModules = g.Count(p => p.IsCompleted)
            })
            .ToListAsync(ct);

        IReadOnlyList<StudentPerformanceResponse> response = grouped
            .Select(g => new StudentPerformanceResponse(
                g.Date.ToString("dd MMM"),
                g.WatchSeconds / 60,
                g.CompletedModules))
            .OrderBy(r => r.Date)
            .ToList();
        return Result.Success(response);
    }
}
