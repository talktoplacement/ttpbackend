using CareerPlatform.Api.Features.Assessments.Dto;

namespace CareerPlatform.Api.Features.Assessments.Service;

/// <summary>
/// The live attempt experience: load, autosave, trial-run, and grade.
///
/// Split from <see cref="IAssessmentService"/> (which owns assessment CRUD and attempt listing)
/// because the responsibilities differ: this one is the exam runtime and the grader, and it is the
/// only place a score may be produced. Every method resolves the caller from the auth context and
/// verifies attempt ownership, so an attempt id from another student is never usable.
/// </summary>
public interface IAssessmentRunnerService
{
    /// <summary>
    /// Loads an in-progress attempt with its questions, saved answers, and server-computed remaining
    /// time. Never includes correct answers or hidden test cases.
    /// </summary>
    Task<Result<AttemptRunnerResponse>> GetRunnerAsync(int attemptId, CancellationToken ct);

    /// <summary>
    /// Upserts the draft answer for one question so progress survives a refresh or disconnect.
    /// Rejected once the attempt is submitted or its time has expired.
    /// </summary>
    Task<Result> SaveAnswerAsync(int attemptId, SaveAnswerRequest request, CancellationToken ct);

    /// <summary>
    /// Runs the submitted code against the VISIBLE sample cases only, for feedback while solving.
    /// Hidden cases are never executed here, so the grader cannot be probed.
    /// </summary>
    Task<Result<RunCodeResponse>> RunSampleTestsAsync(
        int attemptId, RunCodeRequest request, CancellationToken ct);

    /// <summary>
    /// Grades the attempt server-side and finalises it. The score is computed here from stored
    /// answers, correct options, and hidden test cases — never accepted from the client.
    /// </summary>
    Task<Result<AttemptScorecardResponse>> SubmitAsync(int attemptId, CancellationToken ct);

    /// <summary>Returns the scorecard for an already-submitted attempt.</summary>
    Task<Result<AttemptScorecardResponse>> GetScorecardAsync(int attemptId, CancellationToken ct);
}
