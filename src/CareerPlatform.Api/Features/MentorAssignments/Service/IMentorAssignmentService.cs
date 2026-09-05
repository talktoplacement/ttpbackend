using CareerPlatform.Api.Features.MentorAssignments.Dto;

namespace CareerPlatform.Api.Features.MentorAssignments.Service;

public interface IMentorAssignmentService
{
    /// <summary>Admin: assignments, optionally filtered to active only.</summary>
    Task<Result<IReadOnlyList<MentorAssignmentResponse>>> ListAsync(bool activeOnly, CancellationToken ct);

    /// <summary>Admin: students with no active mentor — the valid targets for a new assignment.</summary>
    Task<Result<IReadOnlyList<EligibleStudentResponse>>> ListEligibleStudentsAsync(CancellationToken ct);

    /// <summary>Admin: active, verified mentors with their current assignment load.</summary>
    Task<Result<IReadOnlyList<MentorPoolEntryResponse>>> ListMentorPoolAsync(CancellationToken ct);

    /// <summary>
    /// Admin: create an assignment. Fails when the student already has an active mentor or when
    /// the student / mentor does not exist.
    /// </summary>
    Task<Result<MentorAssignmentResponse>> CreateAsync(CreateMentorAssignmentRequest request, CancellationToken ct);

    Task<Result<MentorAssignmentResponse>> UpdateAsync(int id, UpdateMentorAssignmentRequest request, CancellationToken ct);

    /// <summary>Admin: soft-close an assignment (sets EndedAtUtc). History is preserved.</summary>
    Task<Result<MentorAssignmentResponse>> EndAsync(int id, CancellationToken ct);

    /// <summary>
    /// Student: the caller's own active mentor pairing, or <c>null</c> when none has been assigned.
    ///
    /// A student has at most one active assignment (enforced by <c>CreateAsync</c> and the partial
    /// unique index), so this returns a single record rather than a list. "No mentor yet" is a normal
    /// state, not an error — hence a null payload instead of a 404.
    /// </summary>
    Task<Result<MyMentorResponse?>> GetMyMentorAsync(CancellationToken ct);
}
