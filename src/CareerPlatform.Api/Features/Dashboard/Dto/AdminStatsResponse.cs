namespace CareerPlatform.Api.Features.Dashboard.Dto;

public sealed record AdminStatsResponse(
    int TotalStudents,
    int FreeUsers,
    int MonthlyUsers,
    int YearlyUsers,
    RevenueResponse Revenue);

public sealed record RevenueResponse(decimal TotalAmount, int Transactions);

/// <summary>Registered student projection (superset of legacy fields).</summary>
public sealed record RegisteredStudentResponse(
    string StudentName,
    string EmailID,
    string Mobile,
    string AccountType,
    string CreationDate,
    string PurchaseDate,
    string Status,
    string? Name = null,
    string? Email = null,
    string? Plan = null,
    string? RegisteredDate = null,
    /// <summary>Auth subject id — lets the admin table link to the student's detail page.</summary>
    string? UserId = null);

public sealed record StudentPerformanceResponse(
    string Date,
    int WatchMinutes,
    int CompletedModules);

/// <summary>A single point in a monthly time-series chart (e.g. "Mar" → 180).</summary>
public sealed record AnalyticsPoint(string Label, decimal Value);

/// <summary>
/// Business-analytics rollup for the admin analytics page. Every number is derived from real rows
/// (Transactions for revenue, student UserProfiles for enrollments) over a trailing month window;
/// months with no activity are emitted as genuine zeros so the chart stays continuous.
/// </summary>
public sealed record AnalyticsOverviewResponse(
    decimal TotalRevenue,
    int TotalStudents,
    int PaidStudents,
    IReadOnlyList<AnalyticsPoint> RevenueSeries,
    IReadOnlyList<AnalyticsPoint> EnrollmentSeries);
