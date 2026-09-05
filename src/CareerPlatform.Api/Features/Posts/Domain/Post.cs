using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Posts.Domain;

/// <summary>
/// A user-authored article (the "write an article" surface, à la GeeksForGeeks). Any authenticated
/// user is the author. Content is authored in Markdown and moves through a review lifecycle:
/// <c>draft → in_review → published</c>, with <c>rejected</c> bouncing back to the author for edits.
/// See <see cref="PostStatus"/> for the closed set of states and the allowed transitions.
/// </summary>
public sealed class Post : AuditableEntity<int>
{
    /// <summary>Auth subject (JWT sub) of the author. Set from the principal, never the request.</summary>
    [Required, MaxLength(64)] public string AuthorUserId { get; set; } = string.Empty;

    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;

    /// <summary>URL slug, unique across all posts. Generated from the title on first save.</summary>
    [Required, MaxLength(220)] public string Slug { get; set; } = string.Empty;

    /// <summary>Markdown body. Rendered by the shared MarkdownViewer on the public page.</summary>
    [Required] public string ContentMarkdown { get; set; } = string.Empty;

    /// <summary>Short summary shown in listings; auto-derived from the body when omitted.</summary>
    [MaxLength(500)] public string? Excerpt { get; set; }

    /// <summary>Optional cover/banner image URL.</summary>
    [MaxLength(500)] public string? CoverImageUrl { get; set; }

    /// <summary>Free-text tags, pipe- or comma-separated (e.g. "ABC Company | Interview Experience | Role").</summary>
    [MaxLength(500)] public string? Tags { get; set; }

    /// <summary>Lifecycle state — one of <see cref="PostStatus"/>.</summary>
    [Required, MaxLength(16)] public string Status { get; set; } = PostStatus.Draft;

    public int WordCount { get; set; }
    public int CharCount { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>Admin who last reviewed (approved/rejected) this post.</summary>
    [MaxLength(64)] public string? ReviewedByUserId { get; set; }

    /// <summary>Reviewer's note — the rejection reason shown to the author, or an approval comment.</summary>
    [MaxLength(2000)] public string? ReviewNote { get; set; }
}
