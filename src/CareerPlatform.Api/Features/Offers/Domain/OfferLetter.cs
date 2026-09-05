using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Features.Users.Domain;

namespace CareerPlatform.Api.Features.Offers.Domain;

/// <summary>
/// Offer verification aggregate. Ported from the legacy entity with identical columns; only the
/// base type (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class OfferLetter : AggregateRoot<int>
{
    public string UserId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty; // PDF or JPEG
    public string Status { get; set; } = "Pending"; // "Pending", "Verified", "Rejected"
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public UserProfile? User { get; set; }
}
