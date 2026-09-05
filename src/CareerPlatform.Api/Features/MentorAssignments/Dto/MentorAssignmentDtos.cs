using CareerPlatform.Api.Features.MentorAssignments.Domain;

namespace CareerPlatform.Api.Features.MentorAssignments.Dto;

/// <summary>Assignment row enriched with student + mentor display names for the admin table.</summary>
public sealed record MentorAssignmentResponse(
    int Id,
    string StudentUserId,
    string? StudentName,
    string? StudentEmail,
    int MentorId,
    string? MentorName,
    string? MentorCompany,
    string? CohortName,
    string AssignedAt,
    string? EndedAt,
    string? Notes,
    bool IsActive);

/// <summary>A student who can receive a new assignment (has no active mentor today).</summary>
public sealed record EligibleStudentResponse(
    string UserId, string FullName, string Email, string? PlanName);

/// <summary>A mentor available for assignment, with current active-assignment load.</summary>
public sealed record MentorPoolEntryResponse(
    int MentorId, string Name, string Company, string Role,
    string VerificationStatus, int ActiveAssignmentCount);

/// <summary>
/// The signed-in student's own active mentor pairing, with enough mentor detail to render a profile
/// card. Deliberately omits the admin-only fields (student id/email, notes) that
/// <see cref="MentorAssignmentResponse"/> carries.
/// </summary>
public sealed record MyMentorResponse(
    int AssignmentId,
    int MentorId,
    string MentorName,
    string MentorEmail,
    string MentorCompany,
    string MentorRole,
    string? MentorAvatarUrl,
    string MentorBio,
    IReadOnlyList<string> MentorExpertise,
    string? CohortName,
    string AssignedAt);

public sealed record CreateMentorAssignmentRequest(
    string StudentUserId, int MentorId, string? CohortName, string? Notes);

public sealed record UpdateMentorAssignmentRequest(
    string? CohortName, string? Notes);
