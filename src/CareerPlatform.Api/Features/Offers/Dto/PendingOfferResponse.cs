namespace CareerPlatform.Api.Features.Offers.Dto;

/// <summary>
/// Outward-facing projection of a pending offer. Exposes only display fields; never leaks the
/// <c>OfferLetter</c> entity or its navigation graph. The <c>UploadedAt</c> string is formatted
/// as <c>"dd MMM yyyy HH:mm"</c> and <c>StudentName</c> falls back to <c>"Unknown"</c> when the
/// linked <c>UserProfile</c> is missing (behavior preserved from the legacy shape).
/// </summary>
public sealed record PendingOfferResponse(
    int Id,
    string StudentName,
    string CompanyName,
    string FileUrl,
    string Status,
    string UploadedAt);
