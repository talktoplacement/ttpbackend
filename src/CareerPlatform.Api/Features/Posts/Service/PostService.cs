using System.Text;
using System.Text.RegularExpressions;
using CareerPlatform.Api.Features.Posts.Domain;
using CareerPlatform.Api.Features.Posts.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Posts.Service;

/// <summary>
/// Article-authoring workflow (GeeksForGeeks-style). Any authenticated user drafts, saves, and
/// submits posts; admins approve/reject. Content is Markdown; the author identity, slug, counts,
/// and status transitions are all owned by the server, never trusted from the request.
/// </summary>
internal sealed partial class PostService : IPostService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PostService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    // ── Author self ──────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<PostSummaryResponse>>> ListMineAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<PostSummaryResponse>>(Error.Unauthorized(
                "Post.Unauthorized", "An authenticated user is required."));
        }
        var rows = await _db.Posts.AsNoTracking()
            .Where(p => p.AuthorUserId == userId)
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<PostSummaryResponse> items = rows.Select(p => PostSummaryResponse.From(p)).ToList();
        return Result.Success(items);
    }

    public async Task<Result<PostResponse>> GetMineAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<PostResponse>(Error.Unauthorized(
                "Post.Unauthorized", "An authenticated user is required."));
        }
        var post = await _db.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.AuthorUserId == userId, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        return Result.Success(PostResponse.From(post));
    }

    public async Task<Result<PostResponse>> CreateAsync(PostEditorRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<PostResponse>(Error.Unauthorized(
                "Post.Unauthorized", "An authenticated user is required."));
        }
        var post = new Post
        {
            AuthorUserId = userId,
            Title = request.Title.Trim(),
            Slug = await UniqueSlugAsync(request.Title, excludingId: null, ct),
            ContentMarkdown = request.ContentMarkdown,
            Excerpt = DeriveExcerpt(request.Excerpt, request.ContentMarkdown),
            CoverImageUrl = Trim(request.CoverImageUrl),
            Tags = NormalizeTags(request.Tags),
            Status = PostStatus.Draft,
        };
        ApplyCounts(post);
        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PostResponse.From(post));
    }

    public async Task<Result<PostResponse>> UpdateAsync(int id, PostEditorRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<PostResponse>(Error.Unauthorized(
                "Post.Unauthorized", "An authenticated user is required."));
        }
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id && p.AuthorUserId == userId, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        if (!PostStatus.IsAuthorEditable(post.Status))
        {
            return Result.Failure<PostResponse>(Error.Validation(
                "Post.NotEditable",
                $"A post that is '{post.Status}' cannot be edited. Only drafts and rejected posts are editable."));
        }
        if (!string.Equals(post.Title, request.Title.Trim(), StringComparison.Ordinal))
        {
            post.Slug = await UniqueSlugAsync(request.Title, excludingId: post.Id, ct);
        }
        post.Title = request.Title.Trim();
        post.ContentMarkdown = request.ContentMarkdown;
        post.Excerpt = DeriveExcerpt(request.Excerpt, request.ContentMarkdown);
        post.CoverImageUrl = Trim(request.CoverImageUrl);
        post.Tags = NormalizeTags(request.Tags);
        ApplyCounts(post);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PostResponse.From(post));
    }

    public async Task<Result<PostResponse>> SubmitAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<PostResponse>(Error.Unauthorized(
                "Post.Unauthorized", "An authenticated user is required."));
        }
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id && p.AuthorUserId == userId, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        if (!PostStatus.IsAuthorEditable(post.Status))
        {
            return Result.Failure<PostResponse>(Error.Validation(
                "Post.NotSubmittable",
                $"A post that is '{post.Status}' cannot be submitted for review."));
        }
        if (string.IsNullOrWhiteSpace(post.ContentMarkdown))
        {
            return Result.Failure<PostResponse>(Error.Validation(
                "Post.Empty", "Add some content before submitting for review."));
        }
        post.Status = PostStatus.InReview;
        post.SubmittedAtUtc = DateTime.UtcNow;
        // Clear any prior rejection context on resubmission.
        post.ReviewNote = null;
        post.ReviewedAtUtc = null;
        post.ReviewedByUserId = null;
        await _db.SaveChangesAsync(ct);
        return Result.Success(PostResponse.From(post));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(Error.Unauthorized("Post.Unauthorized", "An authenticated user is required."));
        }
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id && p.AuthorUserId == userId, ct);
        if (post is null)
        {
            return Result.Failure(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        if (post.Status == PostStatus.Published)
        {
            return Result.Failure(Error.Validation(
                "Post.Published", "A published post can't be deleted by the author. Ask an admin to unpublish it first."));
        }
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Admin review ─────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<PostSummaryResponse>>> ListForReviewAsync(string? status, CancellationToken ct)
    {
        var q = _db.Posts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            if (!PostStatus.All.Contains(s))
            {
                return Result.Failure<IReadOnlyList<PostSummaryResponse>>(Error.Validation(
                    "Post.BadStatus", $"Unknown status '{status}'."));
            }
            q = q.Where(p => p.Status == s);
        }
        else
        {
            q = q.Where(p => p.Status == PostStatus.InReview);
        }
        var rows = await q
            .OrderBy(p => p.Status == PostStatus.InReview ? 0 : 1)
            .ThenByDescending(p => p.SubmittedAtUtc ?? p.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        var authors = await ResolveAuthorNamesAsync(rows.Select(r => r.AuthorUserId), ct);
        IReadOnlyList<PostSummaryResponse> items = rows
            .Select(p => PostSummaryResponse.From(p, authors.GetValueOrDefault(p.AuthorUserId)))
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result<PostResponse>> GetForReviewAsync(int id, CancellationToken ct)
    {
        var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        var author = await AuthorNameAsync(post.AuthorUserId, ct);
        return Result.Success(PostResponse.From(post, author));
    }

    public async Task<Result<PostResponse>> ReviewAsync(int id, ReviewPostRequest request, CancellationToken ct)
    {
        var reviewerId = _currentUser.UserId;
        if (string.IsNullOrEmpty(reviewerId))
        {
            return Result.Failure<PostResponse>(Error.Unauthorized(
                "Post.Unauthorized", "An authenticated reviewer is required."));
        }
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        if (post.Status != PostStatus.InReview)
        {
            return Result.Failure<PostResponse>(Error.Validation(
                "Post.NotInReview",
                $"Only posts in review can be actioned. This post is '{post.Status}'."));
        }

        var approve = string.Equals(request.Decision, "approve", StringComparison.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        post.ReviewedByUserId = reviewerId;
        post.ReviewedAtUtc = now;
        post.ReviewNote = Trim(request.Note);
        if (approve)
        {
            post.Status = PostStatus.Published;
            post.PublishedAtUtc = now;
            // Guarantee slug uniqueness among the now-public set (title may have collided as a draft).
            post.Slug = await UniqueSlugAsync(post.Title, excludingId: post.Id, ct);
        }
        else
        {
            post.Status = PostStatus.Rejected;
            post.PublishedAtUtc = null;
        }
        await _db.SaveChangesAsync(ct);
        var author = await AuthorNameAsync(post.AuthorUserId, ct);
        return Result.Success(PostResponse.From(post, author));
    }

    // ── Admin direct authoring (no review step) ────────────────────────────────

    /// <summary>Admin creates an article authored by themselves. Starts as a draft.</summary>
    public async Task<Result<PostResponse>> AdminCreateAsync(PostEditorRequest request, CancellationToken ct)
    {
        var adminId = _currentUser.UserId;
        if (string.IsNullOrEmpty(adminId))
        {
            return Result.Failure<PostResponse>(Error.Unauthorized(
                "Post.Unauthorized", "An authenticated admin is required."));
        }
        var post = new Post
        {
            AuthorUserId = adminId,
            Title = request.Title.Trim(),
            Slug = await UniqueSlugAsync(request.Title, excludingId: null, ct),
            ContentMarkdown = request.ContentMarkdown,
            Excerpt = DeriveExcerpt(request.Excerpt, request.ContentMarkdown),
            CoverImageUrl = Trim(request.CoverImageUrl),
            Tags = NormalizeTags(request.Tags),
            Status = PostStatus.Draft,
        };
        ApplyCounts(post);
        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PostResponse.From(post));
    }

    /// <summary>
    /// Admin edits any article regardless of status (including a published one — edits go live
    /// immediately, preserving the published state).
    /// </summary>
    public async Task<Result<PostResponse>> AdminUpdateAsync(int id, PostEditorRequest request, CancellationToken ct)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        if (!string.Equals(post.Title, request.Title.Trim(), StringComparison.Ordinal))
        {
            post.Slug = await UniqueSlugAsync(request.Title, excludingId: post.Id, ct);
        }
        post.Title = request.Title.Trim();
        post.ContentMarkdown = request.ContentMarkdown;
        post.Excerpt = DeriveExcerpt(request.Excerpt, request.ContentMarkdown);
        post.CoverImageUrl = Trim(request.CoverImageUrl);
        post.Tags = NormalizeTags(request.Tags);
        ApplyCounts(post);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PostResponse.From(post));
    }

    /// <summary>Admin publishes an article directly to the public view, from any status.</summary>
    public async Task<Result<PostResponse>> AdminPublishAsync(int id, CancellationToken ct)
    {
        var adminId = _currentUser.UserId;
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        if (string.IsNullOrWhiteSpace(post.ContentMarkdown))
        {
            return Result.Failure<PostResponse>(Error.Validation(
                "Post.Empty", "Add some content before publishing."));
        }
        var now = DateTime.UtcNow;
        post.Status = PostStatus.Published;
        post.PublishedAtUtc = now;
        post.ReviewedByUserId = adminId;   // self-approved by the publishing admin
        post.ReviewedAtUtc = now;
        post.ReviewNote = null;
        post.Slug = await UniqueSlugAsync(post.Title, excludingId: post.Id, ct);
        await _db.SaveChangesAsync(ct);
        var author = await AuthorNameAsync(post.AuthorUserId, ct);
        return Result.Success(PostResponse.From(post, author));
    }

    /// <summary>Admin pulls an article back off the public view, returning it to draft.</summary>
    public async Task<Result<PostResponse>> AdminUnpublishAsync(int id, CancellationToken ct)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        post.Status = PostStatus.Draft;
        post.PublishedAtUtc = null;
        await _db.SaveChangesAsync(ct);
        var author = await AuthorNameAsync(post.AuthorUserId, ct);
        return Result.Success(PostResponse.From(post, author));
    }

    /// <summary>Admin deletes any article regardless of status.</summary>
    public async Task<Result> AdminDeleteAsync(int id, CancellationToken ct)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return Result.Failure(Error.NotFound("Post.NotFound", $"Post {id} was not found."));
        }
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Public ─────────────────────────────────────────────────────────────--

    public async Task<Result<IReadOnlyList<PostSummaryResponse>>> ListPublishedAsync(string? tag, CancellationToken ct)
    {
        var q = _db.Posts.AsNoTracking().Where(p => p.Status == PostStatus.Published);
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var needle = tag.Trim();
            q = q.Where(p => p.Tags != null && EF.Functions.ILike(p.Tags, $"%{needle}%"));
        }
        var rows = await q
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        var authors = await ResolveAuthorNamesAsync(rows.Select(r => r.AuthorUserId), ct);
        IReadOnlyList<PostSummaryResponse> items = rows
            .Select(p => PostSummaryResponse.From(p, authors.GetValueOrDefault(p.AuthorUserId)))
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result<PostResponse>> GetPublishedBySlugAsync(string slug, CancellationToken ct)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var post = await _db.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == normalized && p.Status == PostStatus.Published, ct);
        if (post is null)
        {
            return Result.Failure<PostResponse>(Error.NotFound(
                "Post.NotFound", $"No published post found for '{slug}'."));
        }
        var author = await AuthorNameAsync(post.AuthorUserId, ct);
        return Result.Success(PostResponse.From(post, author));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────-

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;
        var parts = tags.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : string.Join(" | ", parts);
    }

    private static string? DeriveExcerpt(string? provided, string content)
    {
        if (!string.IsNullOrWhiteSpace(provided)) return provided.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;
        // Strip the most common markdown markers for a readable plain-text preview.
        var text = Regex.Replace(content, @"[#>*_`~\-\[\]\(\)!]", " ");
        text = Whitespace().Replace(text, " ").Trim();
        if (text.Length == 0) return null;
        return text.Length <= 300 ? text : text[..300].TrimEnd() + "…";
    }

    private void ApplyCounts(Post post)
    {
        var text = post.ContentMarkdown ?? string.Empty;
        post.CharCount = text.Length;
        post.WordCount = text.Length == 0
            ? 0
            : Whitespace().Split(text.Trim()).Count(w => w.Length > 0);
    }

    private static string Slugify(string title)
    {
        var lower = (title ?? string.Empty).Trim().ToLowerInvariant();
        var slug = NonSlugChars().Replace(lower, "-").Trim('-');
        if (slug.Length == 0) slug = "post";
        return slug.Length <= 200 ? slug : slug[..200].Trim('-');
    }

    private async Task<string> UniqueSlugAsync(string title, int? excludingId, CancellationToken ct)
    {
        var baseSlug = Slugify(title);
        var candidate = baseSlug;
        var suffix = 2;
        while (await _db.Posts.AnyAsync(
                   p => p.Slug == candidate && (excludingId == null || p.Id != excludingId), ct))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }
        return candidate;
    }

    private async Task<string?> AuthorNameAsync(string authorUserId, CancellationToken ct)
    {
        var profile = await _db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == authorUserId, ct);
        return profile?.FullName;
    }

    private async Task<Dictionary<string, string?>> ResolveAuthorNamesAsync(
        IEnumerable<string> authorIds, CancellationToken ct)
    {
        var ids = authorIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string?>();
        var profiles = await _db.UserProfiles.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);
        return profiles.ToDictionary(p => p.Id, p => (string?)p.FullName);
    }
}
