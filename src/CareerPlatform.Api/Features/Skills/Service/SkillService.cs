using CareerPlatform.Api.Features.Skills.Domain;
using CareerPlatform.Api.Features.Skills.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Skills.Service;

internal sealed class SkillService : ISkillService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SkillService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<SkillsResponse>> GetMySkillsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<SkillsResponse>(Error.Unauthorized(
                "Skills.Unauthorized", "An authenticated user is required."));
        }
        var rows = await _db.UserSkills.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);
        return Result.Success(SkillsResponse.From(rows));
    }

    /// <summary>
    /// Full replacement semantics: the sent list becomes the caller's entire skill set. Wrapped
    /// in a single SaveChanges so a mid-write failure leaves the previous list intact.
    /// </summary>
    public async Task<Result<SkillsResponse>> ReplaceMySkillsAsync(
        ReplaceSkillsRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<SkillsResponse>(Error.Unauthorized(
                "Skills.Unauthorized", "An authenticated user is required."));
        }

        // De-dupe by lowercased skill name within the incoming payload (client-side dupes shouldn't
        // trigger the unique index on (UserId, SkillName)).
        var incoming = request.Skills
            .GroupBy(s => s.SkillName.Trim().ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        var existing = await _db.UserSkills
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        _db.UserSkills.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var item in incoming)
        {
            _db.UserSkills.Add(new UserSkill
            {
                UserId = userId,
                SkillName = item.SkillName.Trim(),
                Category = item.Category.Trim(),
                ProficiencyLevel = NormalizeLevel(item.ProficiencyLevel),
                DisplayOrder = item.DisplayOrder,
            });
        }
        await _db.SaveChangesAsync(ct);

        var saved = await _db.UserSkills.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);
        return Result.Success(SkillsResponse.From(saved));
    }

    private static string NormalizeLevel(string level)
    {
        var trimmed = level.Trim();
        if (trimmed.Length == 0) return "Intermediate";
        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant();
    }
}
