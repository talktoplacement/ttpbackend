using CareerPlatform.Api.Features.Skills.Domain;

namespace CareerPlatform.Api.Features.Skills.Dto;

public sealed record SkillItem(
    string SkillName,
    string Category,
    string ProficiencyLevel,
    int DisplayOrder);

public sealed record SkillsResponse(
    IReadOnlyList<SkillCategoryGroup> Categories,
    int TotalCount)
{
    public static SkillsResponse From(IEnumerable<UserSkill> rows)
    {
        var ordered = rows
            .OrderBy(r => r.Category)
            .ThenBy(r => r.DisplayOrder)
            .ThenBy(r => r.SkillName)
            .ToList();
        var grouped = ordered
            .GroupBy(r => r.Category)
            .Select(g => new SkillCategoryGroup(
                g.Key,
                g.Select(r => new SkillItem(r.SkillName, r.Category, r.ProficiencyLevel, r.DisplayOrder))
                    .ToList()))
            .ToList();
        return new SkillsResponse(grouped, ordered.Count);
    }
}

public sealed record SkillCategoryGroup(string Category, IReadOnlyList<SkillItem> Items);

/// <summary>Full replacement of the caller's skill list. Empty list = clear all skills.</summary>
public sealed record ReplaceSkillsRequest(IReadOnlyList<SkillItem> Skills);
