using CareerPlatform.Api.Features.Assessments.Domain;
using CareerPlatform.Api.Features.Assessments.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Assessments.Service;

internal sealed class AssessmentAuthoringService : IAssessmentAuthoringService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AssessmentAuthoringService> _logger;

    public AssessmentAuthoringService(AppDbContext db, ILogger<AssessmentAuthoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<QuestionBankResponse>> GetBankAsync(int assessmentId, CancellationToken ct)
    {
        if (!await _db.Assessments.AnyAsync(a => a.Id == assessmentId, ct))
        {
            return Result.Failure<QuestionBankResponse>(NotFound(assessmentId));
        }

        var questions = await LoadBankAsync(assessmentId, tracking: false, ct);
        return Result.Success(QuestionBankResponse.From(assessmentId, questions));
    }

    public async Task<Result<QuestionBankResponse>> ReplaceBankAsync(
        int assessmentId, ReplaceQuestionBankRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assessment = await _db.Assessments.FirstOrDefaultAsync(a => a.Id == assessmentId, ct);
        if (assessment is null) return Result.Failure<QuestionBankResponse>(NotFound(assessmentId));

        // Replacing the bank under a graded attempt would silently invalidate its scorecard, whose
        // rows reference question ids that are about to disappear. Clone the assessment instead.
        if (await _db.AssessmentAttempts.AnyAsync(a => a.AssessmentId == assessmentId, ct))
        {
            return Result.Failure<QuestionBankResponse>(Error.Conflict(
                "Assessment.BankLocked",
                "This assessment already has attempts, so its questions can no longer be replaced. " +
                "Create a new assessment instead."));
        }

        var existing = await LoadBankAsync(assessmentId, tracking: true, ct);
        if (existing.Count > 0)
        {
            // Test cases go with the question via the cascade configured on the FK.
            _db.AssessmentQuestions.RemoveRange(existing);
        }

        var orderIndex = 0;
        foreach (var authored in request.Questions)
        {
            var isCoding = string.Equals(
                authored.QuestionType, AssessmentQuestionType.Coding, StringComparison.OrdinalIgnoreCase);

            var question = new AssessmentQuestion
            {
                AssessmentId = assessmentId,
                OrderIndex = orderIndex++,
                QuestionType = authored.QuestionType.Trim().ToLowerInvariant(),
                Title = authored.Title.Trim(),
                PromptMarkdown = authored.PromptMarkdown ?? string.Empty,
                Marks = authored.Marks,
                // Each branch stores only the fields its own type uses, so a question converted from
                // MCQ to coding cannot leave a stale answer key behind.
                OptionsJson = isCoding ? null : JsonPayload.Write(authored.Options?.ToList()),
                CorrectOptionIndex = isCoding ? null : authored.CorrectOptionIndex,
                FunctionName = isCoding ? authored.FunctionName?.Trim() : null,
                StarterCodeJson = isCoding
                    ? JsonPayload.Write(authored.StarterCode?.ToDictionary(k => k.Key, v => v.Value))
                    : null,
                TimeLimitMs = authored.TimeLimitMs ?? DefaultTimeLimitMs,
            };

            if (isCoding && authored.TestCases is { Count: > 0 })
            {
                var caseIndex = 0;
                foreach (var test in authored.TestCases)
                {
                    question.TestCases.Add(new AssessmentTestCase
                    {
                        OrderIndex = caseIndex++,
                        Input = test.Input,
                        ExpectedOutput = test.ExpectedOutput,
                        IsSample = test.IsSample,
                        Weight = Math.Max(1, test.Weight),
                    });
                }
            }

            _db.AssessmentQuestions.Add(question);
        }

        // Derived, never operator-supplied: the catalog card and the runner must agree.
        assessment.QuestionsCount = request.Questions.Count;
        assessment.TotalMarks = request.Questions.Sum(q => q.Marks);
        if (assessment.PassingMarks > assessment.TotalMarks)
        {
            assessment.PassingMarks = assessment.TotalMarks;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Replaced question bank for assessment {AssessmentId}: {Count} questions, {TotalMarks} marks.",
            assessmentId, assessment.QuestionsCount, assessment.TotalMarks);

        var saved = await LoadBankAsync(assessmentId, tracking: false, ct);
        return Result.Success(QuestionBankResponse.From(assessmentId, saved));
    }

    /// <summary>Fallback per-run budget when the author does not state one.</summary>
    private const int DefaultTimeLimitMs = 5000;

    private static Error NotFound(int assessmentId) => Error.NotFound(
        "Assessment.NotFound", $"Assessment {assessmentId} was not found.");

    private async Task<List<AssessmentQuestion>> LoadBankAsync(
        int assessmentId, bool tracking, CancellationToken ct)
    {
        var query = _db.AssessmentQuestions.Include(q => q.TestCases).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query
            .Where(q => q.AssessmentId == assessmentId)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .AsSplitQuery()
            .ToListAsync(ct);
    }
}
