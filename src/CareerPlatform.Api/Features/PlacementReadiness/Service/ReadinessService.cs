using CareerPlatform.Api.Features.PlacementReadiness.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.PlacementReadiness.Service;

internal sealed class ReadinessService : IReadinessService
{
    /// <summary>
    /// The dimensions that make up readiness, with their relative weights.
    ///
    /// Weights are declared once, here, and the response echoes them, so the client never re-implements
    /// the model and the two cannot disagree. Changing the mix is a single-line edit.
    /// </summary>
    private static readonly IReadOnlyList<ComponentDefinition> Definitions = new[]
    {
        new ComponentDefinition("learning", "Course & roadmap progress", 25),
        new ComponentDefinition("assessments", "Assessment performance", 30),
        new ComponentDefinition("interviews", "Mock interview self-assessment", 25),
        new ComponentDefinition("skills", "Declared skill breadth", 10),
        new ComponentDefinition("resume", "Resume readiness", 10),
    };

    /// <summary>
    /// Declared skills that count as full marks for breadth. Beyond this, more skills stop signalling
    /// more readiness.
    /// </summary>
    private const int SkillsForFullCredit = 12;

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ReadinessService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ReadinessResponse>> GetMyReadinessAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<ReadinessResponse>(Error.Unauthorized(
                "Readiness.Unauthorized", "An authenticated student is required."));
        }

        var measurements = new Dictionary<string, Measurement>(StringComparer.Ordinal)
        {
            ["learning"] = await MeasureLearningAsync(userId, ct),
            ["assessments"] = await MeasureAssessmentsAsync(userId, ct),
            ["interviews"] = await MeasureInterviewsAsync(userId, ct),
            ["skills"] = await MeasureSkillsAsync(userId, ct),
            ["resume"] = await MeasureResumeAsync(userId, ct),
        };

        var components = Definitions
            .Select(d =>
            {
                var m = measurements[d.Key];
                return new ReadinessComponentResponse(
                    d.Key, d.Label, d.Weight, m.Score, m.SampleSize, m.Detail);
            })
            .ToList();

        var totalWeight = Definitions.Sum(d => d.Weight);
        var scoredWeight = Definitions
            .Where(d => measurements[d.Key].Score is not null)
            .Sum(d => d.Weight);

        int? overall = null;
        if (scoredWeight > 0)
        {
            // Renormalised over the components that have data. Treating a missing dimension as zero
            // would punish a new student for not having taken an assessment yet; `Coverage` is how the
            // client communicates that the figure rests on partial evidence.
            var weighted = Definitions
                .Where(d => measurements[d.Key].Score is not null)
                .Sum(d => (long)measurements[d.Key].Score!.Value * d.Weight);
            overall = (int)Math.Round((double)weighted / scoredWeight);
        }

        var coverage = totalWeight == 0
            ? 0
            : (int)Math.Round(scoredWeight * 100.0 / totalWeight);

        return Result.Success(new ReadinessResponse(
            overall,
            overall is null ? string.Empty : BandFor(overall.Value),
            coverage,
            components,
            DateTime.UtcNow));
    }

    // ── Component measurements ──────────────────────────────────────────────

    /// <summary>Average completion across every resource the student is tracking.</summary>
    private async Task<Measurement> MeasureLearningAsync(string userId, CancellationToken ct)
    {
        var rows = await _db.LearningProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.PercentComplete)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return Measurement.None("No courses, roadmaps or topics started yet.");
        }

        return new Measurement(
            Clamp((int)Math.Round(rows.Average())),
            rows.Count,
            $"Average completion across {rows.Count} tracked resource{Plural(rows.Count)}.");
    }

    /// <summary>Average marks obtained as a percentage of marks available, over submitted attempts.</summary>
    private async Task<Measurement> MeasureAssessmentsAsync(string userId, CancellationToken ct)
    {
        var attempts = await _db.AssessmentAttempts.AsNoTracking()
            .Where(a => a.UserId == userId
                        && a.SubmittedAtUtc != null
                        && a.Score != null
                        && a.TotalMarks > 0)
            .Select(a => new { Score = a.Score!.Value, a.TotalMarks })
            .ToListAsync(ct);

        if (attempts.Count == 0)
        {
            return Measurement.None("No assessments submitted yet.");
        }

        // Ratio of totals rather than an average of per-attempt percentages, so a 1-mark quiz cannot
        // outweigh a 100-mark paper.
        var earned = attempts.Sum(a => (long)a.Score);
        var available = attempts.Sum(a => (long)a.TotalMarks);

        return new Measurement(
            Clamp((int)Math.Round(earned * 100.0 / available)),
            attempts.Count,
            $"{earned} of {available} marks across {attempts.Count} submitted attempt{Plural(attempts.Count)}.");
    }

    /// <summary>Average score of completed mock-interview sessions that were actually scored.</summary>
    private async Task<Measurement> MeasureInterviewsAsync(string userId, CancellationToken ct)
    {
        var scores = await _db.MockInterviewSessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == "completed" && s.Score != null)
            .Select(s => s.Score!.Value)
            .ToListAsync(ct);

        if (scores.Count == 0)
        {
            return Measurement.None("No scored mock interviews yet.");
        }

        return new Measurement(
            Clamp((int)Math.Round(scores.Average())),
            scores.Count,
            $"Average of {scores.Count} scored session{Plural(scores.Count)}.");
    }

    /// <summary>Breadth of declared skills, saturating at <see cref="SkillsForFullCredit"/>.</summary>
    private async Task<Measurement> MeasureSkillsAsync(string userId, CancellationToken ct)
    {
        var count = await _db.UserSkills.AsNoTracking()
            .CountAsync(s => s.UserId == userId, ct);

        if (count == 0)
        {
            return Measurement.None("No skills declared on your profile yet.");
        }

        return new Measurement(
            Clamp((int)Math.Round(Math.Min(count, SkillsForFullCredit) * 100.0 / SkillsForFullCredit)),
            count,
            $"{count} skill{Plural(count)} declared ({SkillsForFullCredit} for full credit).");
    }

    /// <summary>
    /// Resume readiness. Prefers a real ATS score when one has been computed; otherwise credits the
    /// student for having uploaded a resume at all, which is a genuine (if partial) signal.
    /// </summary>
    private async Task<Measurement> MeasureResumeAsync(string userId, CancellationToken ct)
    {
        var atsScores = await _db.ResumeSubmissions.AsNoTracking()
            .Where(r => r.UserId == userId && r.AtsScore != null)
            .Select(r => r.AtsScore!.Value)
            .ToListAsync(ct);

        if (atsScores.Count > 0)
        {
            return new Measurement(
                Clamp(atsScores.Max()),
                atsScores.Count,
                $"Best ATS score across {atsScores.Count} analysed resume{Plural(atsScores.Count)}.");
        }

        var hasUpload = await _db.StudentResumeUploads.AsNoTracking()
            .AnyAsync(u => u.StudentUserId == userId, ct);

        if (!hasUpload)
        {
            return Measurement.None("No resume uploaded yet.");
        }

        // Half credit, explicitly explained: a resume exists but has never been scanned.
        return new Measurement(50, 1, "Resume uploaded but not yet ATS-analysed.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static int Clamp(int value) => Math.Clamp(value, 0, 100);

    private static string Plural(int count) => count == 1 ? string.Empty : "s";

    /// <summary>
    /// Coarse band for the overall score. Thresholds live beside the calculation so the label always
    /// matches the number it describes.
    /// </summary>
    private static string BandFor(int score) => score switch
    {
        >= 85 => "Interview ready",
        >= 70 => "Nearly ready",
        >= 50 => "Building up",
        _ => "Early stage",
    };

    private sealed record ComponentDefinition(string Key, string Label, int Weight);

    /// <summary>A component's outcome. <c>Score == null</c> means "no data", not "scored zero".</summary>
    private sealed record Measurement(int? Score, int SampleSize, string Detail)
    {
        public static Measurement None(string detail) => new(null, 0, detail);
    }
}
