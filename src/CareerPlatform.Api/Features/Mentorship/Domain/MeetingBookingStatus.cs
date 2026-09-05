namespace CareerPlatform.Api.Features.Mentorship.Domain;

/// <summary>
/// The <c>MeetingBookings.Status</c> lifecycle vocabulary.
///
/// Declared once so the value written at booking time, the value the mentor portal filters on, and
/// the value the grader of "hours mentored" compares against cannot drift. These were previously
/// bare string literals spread across two services with the allowed set recorded only in a trailing
/// code comment — and <see cref="Completed"/> was never written by anything, so every metric derived
/// from it sat permanently at zero.
/// </summary>
public static class MeetingBookingStatus
{
    /// <summary>Booked and upcoming. The state every booking starts in.</summary>
    public const string Scheduled = "Scheduled";

    /// <summary>The session happened. Set by the mentor once the slot's start time has passed.</summary>
    public const string Completed = "Completed";

    /// <summary>Called off by the student or an admin. Terminal.</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>Whether <paramref name="status"/> is one this application recognises.</summary>
    public static bool IsKnown(string? status) =>
        Is(status, Scheduled) || Is(status, Completed) || Is(status, Cancelled);

    /// <summary>
    /// Terminal states: no further transition is allowed, and no join action should be offered.
    /// </summary>
    public static bool IsClosed(string? status) => Is(status, Completed) || Is(status, Cancelled);

    /// <summary>
    /// Case-insensitive comparison. The column is free text rather than an enum, so rows written by
    /// different code paths (or by hand) must still compare equal.
    /// </summary>
    public static bool Is(string? status, string expected) =>
        string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
}
