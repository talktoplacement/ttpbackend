using CareerPlatform.Api.Features.MentorPortal.Dto;

namespace CareerPlatform.Api.Features.MentorPortal.Service;

public interface IMentorPortalService
{
    Task<Result<MentorProfileResponse>> GetProfileAsync(CancellationToken ct);
    Task<Result<MentorProfileResponse>> UpdateProfileAsync(UpdateMentorProfileRequest request, CancellationToken ct);
    Task<Result<MentorOverviewResponse>> GetOverviewAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<MentorSessionResponse>>> ListSessionsAsync(CancellationToken ct);

    /// <summary>
    /// Loads one of the caller's sessions by booking id, returning NotFound when the booking is not
    /// against one of their slots.
    ///
    /// Exists so the session room can fetch exactly what it renders. It previously pulled the whole
    /// session list and searched it client-side, which meant an unknown id silently rendered a blank
    /// room instead of a not-found state, and the payload grew with the mentor's entire history.
    /// </summary>
    Task<Result<MentorSessionResponse>> GetSessionAsync(int bookingId, CancellationToken ct);

    /// <summary>
    /// Marks one of the caller's sessions as completed.
    ///
    /// This is the transition that was missing entirely: bookings were created <c>Scheduled</c> and
    /// could only ever become <c>Cancelled</c>, so nothing ever reached <c>Completed</c> — which left
    /// "hours mentored" pinned at zero and gave students no completed session to review.
    /// </summary>
    Task<Result<MentorSessionResponse>> CompleteSessionAsync(int bookingId, CancellationToken ct);
    Task<Result<IReadOnlyList<MentorMenteeResponse>>> ListStudentsAsync(CancellationToken ct);
    Task<Result<MentorMenteeDetailResponse>> GetStudentAsync(string studentUserId, CancellationToken ct);
    Task<Result<IReadOnlyList<MentorSlotItemResponse>>> ListSlotsAsync(CancellationToken ct);
    Task<Result<MentorSlotItemResponse>> CreateSlotAsync(CreateMentorSlotRequest request, CancellationToken ct);
    Task<Result> DeleteSlotAsync(int id, CancellationToken ct);
    Task<Result<IReadOnlyList<MentorReviewResponse>>> ListReviewsAsync(CancellationToken ct);
}
