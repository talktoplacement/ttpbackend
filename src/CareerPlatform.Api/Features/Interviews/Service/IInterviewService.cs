using CareerPlatform.Api.Features.Interviews.Dto;

namespace CareerPlatform.Api.Features.Interviews.Service;

public interface IInterviewService
{
    // Interview questions (public + admin CRUD)
    Task<Result<IReadOnlyList<InterviewQuestionResponse>>> ListQuestionsAsync(string? topic, string? difficulty, bool publishedOnly, CancellationToken ct);
    Task<Result<InterviewQuestionResponse>> GetQuestionByIdAsync(int id, CancellationToken ct);
    Task<Result<InterviewQuestionResponse>> CreateQuestionAsync(CreateInterviewQuestionRequest request, CancellationToken ct);
    Task<Result<InterviewQuestionResponse>> UpdateQuestionAsync(int id, UpdateInterviewQuestionRequest request, CancellationToken ct);
    Task<Result> DeleteQuestionAsync(int id, CancellationToken ct);

    /// <summary>
    /// The question bank grouped into topics, with real counts, the company tags actually present, and
    /// the caller's own session history per topic. Backs the interviews hub, which previously rendered
    /// a hand-written array of tracks with invented question counts and enrollment flags.
    /// </summary>
    Task<Result<IReadOnlyList<InterviewTopicResponse>>> ListTopicsAsync(CancellationToken ct);

    // Mock interview sessions ("me" scope)
    Task<Result<IReadOnlyList<MockInterviewSessionResponse>>> ListMySessionsAsync(CancellationToken ct);
    Task<Result<MockInterviewSessionResponse>> CreateMySessionAsync(CreateInterviewSessionRequest request, CancellationToken ct);
    Task<Result<MockInterviewSessionResponse>> UpdateMySessionAsync(int id, UpdateInterviewSessionRequest request, CancellationToken ct);

    /// <summary>
    /// Admin: every student's mock-interview sessions, newest first. Optional filters on status
    /// and topic so the admin queue can be narrowed without client-side filtering.
    /// </summary>
    Task<Result<IReadOnlyList<AdminMockInterviewSessionResponse>>> ListAllSessionsAsync(
        string? status, string? topic, CancellationToken ct);
}
