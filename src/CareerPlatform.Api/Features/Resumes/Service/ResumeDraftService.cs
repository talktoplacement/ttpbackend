using System.Text.Json;
using CareerPlatform.Api.Features.Resumes.Domain;
using CareerPlatform.Api.Features.Resumes.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Resumes.Service;

internal sealed class ResumeDraftService : IResumeDraftService
{
    /// <summary>
    /// Cap on the stored builder document. Generous enough for a long resume with many bullet points,
    /// small enough that a runaway client cannot fill the column.
    /// </summary>
    private const int MaxContentBytes = 64 * 1024;

    /// <summary>Keeps one student's draft list bounded.</summary>
    private const int MaxDraftsPerUser = 25;

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ResumeDraftService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ResumeDraftResponse>>> ListMineAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<IReadOnlyList<ResumeDraftResponse>>(Unauthorized());
        }

        var rows = await Mine(userId).AsNoTracking()
            .OrderByDescending(d => d.LastEditedAtUtc)
            .ThenByDescending(d => d.Id)
            .ToListAsync(ct);

        IReadOnlyList<ResumeDraftResponse> mapped = rows.Select(ResumeDraftResponse.From).ToList();
        return Result.Success(mapped);
    }

    public async Task<Result<ResumeDraftResponse>> GetMineAsync(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<ResumeDraftResponse>(Unauthorized());
        }

        var row = await Mine(userId).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        return row is null
            ? Result.Failure<ResumeDraftResponse>(NotFound(id))
            : Result.Success(ResumeDraftResponse.From(row));
    }

    public async Task<Result<ResumeDraftResponse>> CreateMineAsync(
        CreateResumeDraftRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<ResumeDraftResponse>(Unauthorized());
        }

        var count = await Mine(userId).CountAsync(ct);
        if (count >= MaxDraftsPerUser)
        {
            return Result.Failure<ResumeDraftResponse>(Error.Validation(
                "ResumeDraft.TooMany",
                $"You already have {MaxDraftsPerUser} drafts. Delete one before creating another."));
        }

        var template = await ResolveTemplateCodeAsync(request.TemplateCode, ct);
        if (template.IsFailure)
        {
            return Result.Failure<ResumeDraftResponse>(template.Error);
        }

        var content = SerialiseContent(request.Content);
        if (content.IsFailure)
        {
            return Result.Failure<ResumeDraftResponse>(content.Error);
        }

        var now = DateTime.UtcNow;
        var draft = new ResumeDraft
        {
            UserId = userId,
            Title = request.Title.Trim(),
            TemplateCode = template.Value,
            ContentJson = content.Value,
            LastEditedAtUtc = now,
        };

        _db.ResumeDrafts.Add(draft);
        await _db.SaveChangesAsync(ct);

        return Result.Success(ResumeDraftResponse.From(draft));
    }

    public async Task<Result<ResumeDraftResponse>> UpdateMineAsync(
        int id, UpdateResumeDraftRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<ResumeDraftResponse>(Unauthorized());
        }

        // Ownership lives in the predicate, so another student's draft is indistinguishable from a
        // non-existent one.
        var draft = await Mine(userId).FirstOrDefaultAsync(d => d.Id == id, ct);
        if (draft is null)
        {
            return Result.Failure<ResumeDraftResponse>(NotFound(id));
        }

        if (request.Title is not null)
        {
            draft.Title = request.Title.Trim();
        }

        if (request.TemplateCode is not null)
        {
            var template = await ResolveTemplateCodeAsync(request.TemplateCode, ct);
            if (template.IsFailure)
            {
                return Result.Failure<ResumeDraftResponse>(template.Error);
            }
            draft.TemplateCode = template.Value;
        }

        if (request.Content is not null)
        {
            var content = SerialiseContent(request.Content);
            if (content.IsFailure)
            {
                return Result.Failure<ResumeDraftResponse>(content.Error);
            }
            draft.ContentJson = content.Value;
        }

        draft.LastEditedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result.Success(ResumeDraftResponse.From(draft));
    }

    public async Task<Result> DeleteMineAsync(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure(Unauthorized());
        }

        var draft = await Mine(userId).FirstOrDefaultAsync(d => d.Id == id, ct);
        if (draft is null)
        {
            return Result.Failure(NotFound(id));
        }

        _db.ResumeDrafts.Remove(draft);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private IQueryable<ResumeDraft> Mine(string userId) =>
        _db.ResumeDrafts.Where(d => d.UserId == userId);

    private bool TryGetUserId(out string userId)
    {
        userId = _currentUser.UserId ?? string.Empty;
        return userId.Length > 0;
    }

    private static Error Unauthorized() => Error.Unauthorized(
        "ResumeDraft.Unauthorized", "An authenticated user is required.");

    private static Error NotFound(int id) => Error.NotFound(
        "ResumeDraft.NotFound", $"Draft {id} was not found on your account.");

    /// <summary>
    /// Confirms the requested template exists and is published, and returns its canonical code so a
    /// case difference in the request cannot create a draft the template lookup will later miss.
    /// </summary>
    private async Task<Result<string>> ResolveTemplateCodeAsync(string requested, CancellationToken ct)
    {
        var trimmed = (requested ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result.Failure<string>(Error.Validation(
                "ResumeDraft.TemplateRequired", "A template code is required."));
        }

        var code = await _db.ResumeTemplates.AsNoTracking()
            .Where(t => t.IsPublished && t.Code.ToLower() == trimmed.ToLower())
            .Select(t => t.Code)
            .FirstOrDefaultAsync(ct);

        if (code is null)
        {
            return Result.Failure<string>(Error.Validation(
                "ResumeDraft.UnknownTemplate",
                $"'{trimmed}' is not a published resume template."));
        }

        return Result.Success(code);
    }

    /// <summary>
    /// Serialises the builder document. Only an object is accepted at the root: an array or scalar
    /// would still be valid JSON but could not carry a resume, and rejecting it here keeps every
    /// stored row loadable by the builder.
    /// </summary>
    private static Result<string> SerialiseContent(JsonElement? content)
    {
        if (content is null || content.Value.ValueKind == JsonValueKind.Undefined
            || content.Value.ValueKind == JsonValueKind.Null)
        {
            return Result.Success("{}");
        }

        if (content.Value.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<string>(Error.Validation(
                "ResumeDraft.InvalidContent", "Draft content must be a JSON object."));
        }

        var json = content.Value.GetRawText();
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxContentBytes)
        {
            return Result.Failure<string>(Error.Validation(
                "ResumeDraft.ContentTooLarge",
                $"Draft content must be under {MaxContentBytes / 1024} KB."));
        }

        return Result.Success(json);
    }
}
