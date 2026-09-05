using CareerPlatform.Api.Features.Interviews.Domain;

namespace CareerPlatform.Api.Features.Interviews.Dto;

public sealed record InterviewRubricResponse(
    int Id, string Title, string Description, int Weight, int DisplayOrder, bool IsPublished)
{
    public static InterviewRubricResponse From(InterviewRubric r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new InterviewRubricResponse(
            r.Id, r.Title, r.Description, r.Weight, r.DisplayOrder, r.IsPublished);
    }
}

/// <summary>Create/update body for an interview rubric axis.</summary>
public sealed record UpsertInterviewRubricRequest(
    string Title, string? Description, int Weight, int DisplayOrder, bool IsPublished);
