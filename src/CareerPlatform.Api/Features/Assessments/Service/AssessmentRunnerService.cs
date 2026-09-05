using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.Assessments.Domain;
using CareerPlatform.Api.Features.Assessments.Dto;
using CareerPlatform.Api.Infrastructure.CodeExecution;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Assessments.Service;

/// <summary>
/// The exam runtime and grader.
///
/// Two invariants drive the whole design:
///  1. <b>The server scores.</b> Nothing the client sends contributes to a score. Marks are computed
///     here from the stored answer, the stored correct option, and the hidden test cases. The
///     previous implementation accepted a <c>Score</c> in the request body, so any student could
///     submit a perfect result.
///  2. <b>The answer key never leaves the server.</b> Correct options and non-sample test cases are
///     excluded from every response projection, and the interactive run path executes sample cases
///     only.
///
/// Time limits are also enforced server-side: the deadline is derived from the stored start time and
/// the assessment duration, so editing a client-side countdown achieves nothing.
/// </summary>
internal sealed class AssessmentRunnerService : IAssessmentRunnerService
{
    private static readonly Error Unauthorized = Error.Unauthorized(
        "Assessment.Unauthorized", "An authenticated user is required.");

    /// <summary>Grace period allowed past the deadline to absorb clock skew and request latency.</summary>
    private const int SubmissionGraceSeconds = 30;

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICodeExecutor _codeExecutor;
    private readonly CodeExecutionOptions _codeExecutionOptions;
    private readonly ILogger<AssessmentRunnerService> _logger;

    public AssessmentRunnerService(
        AppDbContext db,
        ICurrentUser currentUser,
        ICodeExecutor codeExecutor,
        IOptions<CodeExecutionOptions> codeExecutionOptions,
        ILogger<AssessmentRunnerService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _codeExecutor = codeExecutor;
        _codeExecutionOptions = codeExecutionOptions.Value;
        _logger = logger;
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    public async Task<Result<AttemptRunnerResponse>> GetRunnerAsync(int attemptId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure<AttemptRunnerResponse>(Unauthorized);

        var attempt = await LoadOwnedAttemptAsync(attemptId, userId, tracking: false, ct);
        if (attempt is null)
        {
            return Result.Failure<AttemptRunnerResponse>(AttemptNotFound(attemptId));
        }

        var questions = await LoadQuestionsAsync(attempt.AssessmentId, ct);
        var saved = await _db.AssessmentAttemptAnswers.AsNoTracking()
            .Where(a => a.AttemptId == attemptId)
            .Select(a => new RunnerSavedAnswerResponse(
                a.QuestionId, a.SelectedOptionIndex, a.Language, a.SourceCode))
            .ToListAsync(ct);

        var durationMinutes = attempt.Assessment?.DurationMinutes ?? 0;

        return Result.Success(new AttemptRunnerResponse(
            AttemptId: attempt.Id,
            AssessmentId: attempt.AssessmentId,
            AssessmentSlug: attempt.Assessment?.Slug ?? string.Empty,
            AssessmentTitle: attempt.Assessment?.Title ?? string.Empty,
            DurationMinutes: durationMinutes,
            StartedAt: attempt.StartedAtUtc.ToString("O"),
            SubmittedAt: attempt.SubmittedAtUtc?.ToString("O"),
            RemainingSeconds: RemainingSeconds(attempt.StartedAtUtc, durationMinutes),
            TotalMarks: attempt.TotalMarks,
            PassingMarks: attempt.PassingMarks,
            IsSubmitted: attempt.SubmittedAtUtc is not null,
            CodeExecutionEnabled: _codeExecutor.IsEnabled,
            Languages: _codeExecutor.SupportedLanguages
                .Select(l => new CodeLanguageResponse(l.Id, l.Label)).ToList(),
            Questions: questions.Select(RunnerQuestionResponse.From).ToList(),
            SavedAnswers: saved));
    }

    // ── Autosave ─────────────────────────────────────────────────────────────

    public async Task<Result> SaveAnswerAsync(
        int attemptId, SaveAnswerRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure(Unauthorized);

        var attempt = await LoadOwnedAttemptAsync(attemptId, userId, tracking: false, ct);
        if (attempt is null) return Result.Failure(AttemptNotFound(attemptId));
        if (attempt.SubmittedAtUtc is not null)
        {
            return Result.Failure(AlreadySubmitted);
        }
        if (IsExpired(attempt))
        {
            return Result.Failure(Error.Validation(
                "Assessment.TimeExpired", "The time limit for this attempt has elapsed."));
        }

        // The question must belong to this attempt's assessment; otherwise a caller could attach
        // answers from an unrelated assessment.
        var belongs = await _db.AssessmentQuestions.AsNoTracking()
            .AnyAsync(q => q.Id == request.QuestionId && q.AssessmentId == attempt.AssessmentId, ct);
        if (!belongs)
        {
            return Result.Failure(Error.Validation(
                "Assessment.QuestionNotInAssessment",
                "That question does not belong to this assessment."));
        }

        var answer = await _db.AssessmentAttemptAnswers
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == request.QuestionId, ct);

        if (answer is null)
        {
            answer = new AssessmentAttemptAnswer
            {
                AttemptId = attemptId,
                QuestionId = request.QuestionId,
            };
            _db.AssessmentAttemptAnswers.Add(answer);
        }

        answer.SelectedOptionIndex = request.SelectedOptionIndex;
        answer.Language = Trim(request.Language, 32);
        answer.SourceCode = request.SourceCode;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two rapid autosaves for the same question can race the unique index. The later write
            // is the one we want; retry once against the now-existing row.
            _db.ChangeTracker.Clear();
            var existing = await _db.AssessmentAttemptAnswers
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == request.QuestionId, ct);
            if (existing is null) throw;

            existing.SelectedOptionIndex = request.SelectedOptionIndex;
            existing.Language = Trim(request.Language, 32);
            existing.SourceCode = request.SourceCode;
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    // ── Interactive run (sample cases only) ──────────────────────────────────

    public async Task<Result<RunCodeResponse>> RunSampleTestsAsync(
        int attemptId, RunCodeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure<RunCodeResponse>(Unauthorized);

        var attempt = await LoadOwnedAttemptAsync(attemptId, userId, tracking: false, ct);
        if (attempt is null) return Result.Failure<RunCodeResponse>(AttemptNotFound(attemptId));
        if (attempt.SubmittedAtUtc is not null) return Result.Failure<RunCodeResponse>(AlreadySubmitted);

        var question = await _db.AssessmentQuestions.AsNoTracking()
            .Include(q => q.TestCases)
            .FirstOrDefaultAsync(
                q => q.Id == request.QuestionId && q.AssessmentId == attempt.AssessmentId, ct);
        if (question is null)
        {
            return Result.Failure<RunCodeResponse>(Error.NotFound(
                "Assessment.QuestionNotFound", "That question was not found in this assessment."));
        }
        if (!question.IsCoding)
        {
            return Result.Failure<RunCodeResponse>(Error.Validation(
                "Assessment.NotCodingQuestion", "Only coding questions can be executed."));
        }
        if (!_codeExecutor.IsEnabled)
        {
            return Result.Success(new RunCodeResponse(
                ExecutionAvailable: false,
                FailureReason: "Code execution is not available on this environment.",
                PassedCount: 0, TotalCount: 0, Results: Array.Empty<SampleRunResult>()));
        }

        // ONLY sample cases: running hidden cases here would let a student reverse-engineer the grader.
        var samples = (question.TestCases ?? new List<AssessmentTestCase>())
            .Where(t => t.IsSample)
            .OrderBy(t => t.OrderIndex)
            .ToList();

        var results = new List<SampleRunResult>(samples.Count);
        string? failureReason = null;

        foreach (var test in samples)
        {
            var execution = await _codeExecutor.ExecuteAsync(
                new CodeExecutionRequest(request.Language, request.SourceCode, test.Input, question.TimeLimitMs),
                ct);

            if (execution.FailureReason is not null && execution.Stdout.Length == 0)
            {
                failureReason ??= execution.FailureReason;
            }

            results.Add(new SampleRunResult(
                TestCaseId: test.Id,
                Input: test.Input,
                ExpectedOutput: test.ExpectedOutput,
                ActualOutput: execution.Stdout,
                Passed: execution.Succeeded && OutputMatches(execution.Stdout, test.ExpectedOutput),
                TimedOut: execution.TimedOut,
                Stderr: string.IsNullOrWhiteSpace(execution.Stderr) ? null : execution.Stderr));
        }

        return Result.Success(new RunCodeResponse(
            ExecutionAvailable: true,
            FailureReason: failureReason,
            PassedCount: results.Count(r => r.Passed),
            TotalCount: results.Count,
            Results: results));
    }

    // ── Submit + grade ───────────────────────────────────────────────────────

    public async Task<Result<AttemptScorecardResponse>> SubmitAsync(int attemptId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure<AttemptScorecardResponse>(Unauthorized);

        var attempt = await LoadOwnedAttemptAsync(attemptId, userId, tracking: true, ct);
        if (attempt is null) return Result.Failure<AttemptScorecardResponse>(AttemptNotFound(attemptId));

        // Re-submitting returns the existing scorecard instead of erroring: the student may have lost
        // the response to a flaky connection, and grading twice would be wasteful and confusing.
        if (attempt.SubmittedAtUtc is not null)
        {
            return await GetScorecardAsync(attemptId, ct);
        }

        var questions = await LoadQuestionsAsync(attempt.AssessmentId, ct);
        var answers = await _db.AssessmentAttemptAnswers
            .Where(a => a.AttemptId == attemptId)
            .ToListAsync(ct);
        var answerByQuestion = answers.ToDictionary(a => a.QuestionId);

        var now = DateTime.UtcNow;
        var breakdown = new List<QuestionScoreResponse>(questions.Count);
        var totalScore = 0;

        foreach (var question in questions)
        {
            answerByQuestion.TryGetValue(question.Id, out var answer);

            // Materialise a row even for an unanswered question so the scorecard is complete and the
            // grading run is fully auditable.
            if (answer is null)
            {
                answer = new AssessmentAttemptAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = question.Id,
                };
                _db.AssessmentAttemptAnswers.Add(answer);
            }

            var graded = question.IsCoding
                ? await GradeCodingAsync(question, answer, ct)
                : GradeMultipleChoice(question, answer);

            answer.AwardedMarks = graded.AwardedMarks;
            answer.IsCorrect = graded.IsCorrect;
            answer.PassedTestCount = graded.PassedTestCount;
            answer.TotalTestCount = graded.TotalTestCount;
            answer.EvaluatedAtUtc = now;

            totalScore += graded.AwardedMarks;
            breakdown.Add(new QuestionScoreResponse(
                question.Id, question.Title, question.QuestionType, question.Marks,
                graded.AwardedMarks, graded.IsCorrect, graded.PassedTestCount, graded.TotalTestCount));
        }

        // Total marks are recomputed from the question bank rather than trusting the snapshot, so an
        // assessment edited after the attempt started still produces a coherent percentage.
        var totalMarks = questions.Sum(q => q.Marks);
        if (totalMarks <= 0) totalMarks = attempt.TotalMarks;

        attempt.SubmittedAtUtc = now;
        attempt.Score = totalScore;
        attempt.TotalMarks = totalMarks;
        attempt.Passed = totalScore >= attempt.PassingMarks;
        attempt.TimeTakenMinutes = Math.Max(
            0, (int)Math.Round((now - attempt.StartedAtUtc).TotalMinutes));

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Graded attempt {AttemptId}: {Score}/{TotalMarks} (passed={Passed}).",
            attemptId, totalScore, totalMarks, attempt.Passed);

        return Result.Success(new AttemptScorecardResponse(
            attemptId, totalScore, totalMarks, attempt.PassingMarks,
            attempt.Passed ?? false, attempt.TimeTakenMinutes ?? 0,
            now.ToString("O"), breakdown));
    }

    public async Task<Result<AttemptScorecardResponse>> GetScorecardAsync(
        int attemptId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure<AttemptScorecardResponse>(Unauthorized);

        var attempt = await LoadOwnedAttemptAsync(attemptId, userId, tracking: false, ct);
        if (attempt is null) return Result.Failure<AttemptScorecardResponse>(AttemptNotFound(attemptId));
        if (attempt.SubmittedAtUtc is null)
        {
            return Result.Failure<AttemptScorecardResponse>(Error.Validation(
                "Assessment.AttemptNotSubmitted", "This attempt has not been submitted yet."));
        }

        var breakdown = await _db.AssessmentAttemptAnswers.AsNoTracking()
            .Where(a => a.AttemptId == attemptId)
            .Include(a => a.Question)
            .OrderBy(a => a.Question!.OrderIndex)
            .Select(a => new QuestionScoreResponse(
                a.QuestionId,
                a.Question!.Title,
                a.Question!.QuestionType,
                a.Question!.Marks,
                a.AwardedMarks,
                a.IsCorrect ?? false,
                a.PassedTestCount,
                a.TotalTestCount))
            .ToListAsync(ct);

        return Result.Success(new AttemptScorecardResponse(
            attempt.Id, attempt.Score ?? 0, attempt.TotalMarks, attempt.PassingMarks,
            attempt.Passed ?? false, attempt.TimeTakenMinutes ?? 0,
            attempt.SubmittedAtUtc.Value.ToString("O"), breakdown));
    }

    // ── Grading ──────────────────────────────────────────────────────────────

    private sealed record GradeOutcome(
        int AwardedMarks, bool IsCorrect, int PassedTestCount, int TotalTestCount);

    /// <summary>
    /// All-or-nothing on the stored correct index. An unanswered or out-of-range choice scores zero.
    /// </summary>
    private static GradeOutcome GradeMultipleChoice(
        AssessmentQuestion question, AssessmentAttemptAnswer answer)
    {
        var isCorrect =
            question.CorrectOptionIndex.HasValue &&
            answer.SelectedOptionIndex.HasValue &&
            answer.SelectedOptionIndex.Value == question.CorrectOptionIndex.Value;

        return new GradeOutcome(isCorrect ? question.Marks : 0, isCorrect, isCorrect ? 1 : 0, 1);
    }

    /// <summary>
    /// Runs every test case (sample AND hidden) and apportions the question's marks by the weight of
    /// the cases that passed, so partial credit is proportional to real coverage. Full marks require
    /// every case to pass.
    /// </summary>
    private async Task<GradeOutcome> GradeCodingAsync(
        AssessmentQuestion question, AssessmentAttemptAnswer answer, CancellationToken ct)
    {
        var tests = (question.TestCases ?? new List<AssessmentTestCase>())
            .OrderBy(t => t.OrderIndex)
            .ToList();

        if (tests.Count == 0 ||
            string.IsNullOrWhiteSpace(answer.SourceCode) ||
            string.IsNullOrWhiteSpace(answer.Language))
        {
            return new GradeOutcome(0, false, 0, tests.Count);
        }

        if (!_codeExecutor.IsEnabled)
        {
            // No sandbox: award nothing but record the attempt so the gap is visible on the scorecard
            // rather than silently reading as a wrong answer.
            _logger.LogWarning(
                "Cannot grade coding question {QuestionId}: no code executor is configured.", question.Id);
            return new GradeOutcome(0, false, 0, tests.Count);
        }

        var totalWeight = tests.Sum(t => Math.Max(1, t.Weight));
        var earnedWeight = 0;
        var passedCount = 0;

        foreach (var test in tests)
        {
            var execution = await _codeExecutor.ExecuteAsync(
                new CodeExecutionRequest(
                    answer.Language!, answer.SourceCode!, test.Input, question.TimeLimitMs),
                ct);

            if (execution.Succeeded && OutputMatches(execution.Stdout, test.ExpectedOutput))
            {
                passedCount++;
                earnedWeight += Math.Max(1, test.Weight);
            }
        }

        var awarded = totalWeight == 0
            ? 0
            : (int)Math.Round(question.Marks * (earnedWeight / (double)totalWeight),
                MidpointRounding.ToZero);

        return new GradeOutcome(awarded, passedCount == tests.Count, passedCount, tests.Count);
    }

    /// <summary>
    /// Compares program output to the expected value, ignoring trailing whitespace on each line and
    /// at the end. Students should not fail a correct solution over a stray newline.
    /// </summary>
    private static bool OutputMatches(string actual, string expected) =>
        string.Equals(Canonicalize(actual), Canonicalize(expected), StringComparison.Ordinal);

    private static string Canonicalize(string value) =>
        string.Join('\n', (value ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(line => line.TrimEnd()))
            .TrimEnd();

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static Error AttemptNotFound(int attemptId) => Error.NotFound(
        "Assessment.AttemptNotFound", $"Attempt {attemptId} was not found.");

    private static readonly Error AlreadySubmitted = Error.Conflict(
        "Assessment.AttemptAlreadySubmitted", "This attempt has already been submitted.");

    /// <summary>
    /// Loads an attempt only when it belongs to <paramref name="userId"/>. Centralised so no call
    /// site can forget the ownership predicate — a missing check here would be a direct IDOR.
    /// </summary>
    private async Task<AssessmentAttempt?> LoadOwnedAttemptAsync(
        int attemptId, string userId, bool tracking, CancellationToken ct)
    {
        var query = _db.AssessmentAttempts.Include(a => a.Assessment).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct);
    }

    private async Task<List<AssessmentQuestion>> LoadQuestionsAsync(int assessmentId, CancellationToken ct) =>
        await _db.AssessmentQuestions.AsNoTracking()
            .Include(q => q.TestCases)
            .Where(q => q.AssessmentId == assessmentId)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .AsSplitQuery()
            .ToListAsync(ct);

    /// <summary>Whether the attempt is past its server-computed deadline (plus grace).</summary>
    private static bool IsExpired(AssessmentAttempt attempt)
    {
        var duration = attempt.Assessment?.DurationMinutes ?? 0;
        if (duration <= 0) return false; // untimed assessment
        var deadline = attempt.StartedAtUtc.AddMinutes(duration).AddSeconds(SubmissionGraceSeconds);
        return DateTime.UtcNow > deadline;
    }

    private static int RemainingSeconds(DateTime startedAtUtc, int durationMinutes)
    {
        if (durationMinutes <= 0) return 0; // 0 means "untimed" to the client
        var remaining = startedAtUtc.AddMinutes(durationMinutes) - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? (int)remaining.TotalSeconds : 0;
    }

    private static string? Trim(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= max ? value.Trim() : value.Trim()[..max];
}
