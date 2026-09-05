using CareerPlatform.Api.Features.Mentorship.Dto;

namespace CareerPlatform.Api.Features.Mentorship.Service;

public interface IMentorshipService
{
    // Catalog + slots (student)
    Task<Result<IReadOnlyList<MentorResponse>>> ListMentorsAsync(string? expertise, bool activeOnly, CancellationToken ct);

    /// <summary>
    /// Anonymous-safe catalog of verified + active mentors for the public marketing page. Returns
    /// <see cref="PublicMentorResponse"/>, which omits the mentor's email.
    /// </summary>
    Task<Result<IReadOnlyList<PublicMentorResponse>>> ListPublicMentorsAsync(string? expertise, CancellationToken ct);
    Task<Result<IReadOnlyList<MentorSlotResponse>>> ListMentorSlotsAsync(int mentorId, CancellationToken ct);

    // Booking (student)
    Task<Result<MentorBookingResponse>> BookAsync(BookMentorSlotRequest body, CancellationToken ct);
    Task<Result<IReadOnlyList<MentorBookingResponse>>> ListMyBookingsAsync(CancellationToken ct);

    /// <summary>
    /// Records the student's rating for one of their COMPLETED bookings and refreshes the mentor's
    /// aggregate rating.
    ///
    /// This is the only writer of <c>MentorReviews</c>. Without it the mentor feedback page and the
    /// mentor's average rating could never show anything, because no code path created a review row.
    /// </summary>
    Task<Result<MentorBookingResponse>> SubmitReviewAsync(
        int bookingId, SubmitMentorReviewRequest body, CancellationToken ct);

    // Admin slot CRUD
    Task<Result<IReadOnlyList<MentorSlotResponse>>> CreateSlotsAsync(CreateMentorSlotsRequest body, CancellationToken ct);
    Task<Result> DeleteSlotAsync(int id, CancellationToken ct);

    // Admin bookings
    Task<Result<IReadOnlyList<MentorBookingResponse>>> ListAdminBookingsAsync(CancellationToken ct);
    Task<Result> CancelBookingAsync(int id, CancellationToken ct);

    // Admin mentor lifecycle
    Task<Result<MentorResponse>> GetMentorByIdAsync(int id, CancellationToken ct);
    Task<Result<MentorResponse>> OnboardAsync(OnboardMentorRequest body, CancellationToken ct);
    Task<Result<MentorResponse>> UpdateAsync(UpdateMentorRequest body, CancellationToken ct);
}
