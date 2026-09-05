namespace CareerPlatform.Api.Features.Posts.Domain;

/// <summary>
/// The closed set of post lifecycle states plus the transition rules. Kept as string constants
/// (not an enum) to match the persisted <c>character varying</c> column and the rest of the codebase.
///
/// Transitions:
///   draft      → in_review            (author "Submit for Review")
///   rejected   → in_review            (author edits & resubmits)
///   in_review  → published | rejected (admin review)
///   published  → (terminal; edits create a new revision — out of scope here)
/// </summary>
public static class PostStatus
{
    public const string Draft = "draft";
    public const string InReview = "in_review";
    public const string Published = "published";
    public const string Rejected = "rejected";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Draft, InReview, Published, Rejected };

    /// <summary>States an author may still edit / resubmit.</summary>
    public static bool IsAuthorEditable(string status) =>
        status is Draft or Rejected;
}
