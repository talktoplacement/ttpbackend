using CareerPlatform.Api.Features.Interviews.Domain;
using CareerPlatform.Api.Features.Interviews.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Interviews.Service;

internal sealed class InterviewRubricService : IInterviewRubricService
{
    private readonly AppDbContext _db;
    public InterviewRubricService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<InterviewRubricResponse>>> ListAsync(bool publishedOnly, CancellationToken ct)
    {
        var query = _db.InterviewRubrics.AsNoTracking();
        if (publishedOnly) query = query.Where(r => r.IsPublished);
        var rows = await query
            .OrderBy(r => r.DisplayOrder).ThenBy(r => r.Id)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        return Result.Success((IReadOnlyList<InterviewRubricResponse>)rows.Select(InterviewRubricResponse.From).ToList());
    }

    public async Task<Result<InterviewRubricResponse>> GetAsync(int id, CancellationToken ct)
    {
        var r = await _db.InterviewRubrics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null)
        {
            return Result.Failure<InterviewRubricResponse>(Error.NotFound(
                "InterviewRubric.NotFound", $"Rubric {id} was not found."));
        }
        return Result.Success(InterviewRubricResponse.From(r));
    }

    public async Task<Result<InterviewRubricResponse>> CreateAsync(UpsertInterviewRubricRequest r, CancellationToken ct)
    {
        var rubric = new InterviewRubric
        {
            Title = r.Title.Trim(),
            Description = r.Description?.Trim() ?? string.Empty,
            Weight = r.Weight,
            DisplayOrder = r.DisplayOrder,
            IsPublished = r.IsPublished,
        };
        _db.InterviewRubrics.Add(rubric);
        await _db.SaveChangesAsync(ct);
        return Result.Success(InterviewRubricResponse.From(rubric));
    }

    public async Task<Result<InterviewRubricResponse>> UpdateAsync(int id, UpsertInterviewRubricRequest r, CancellationToken ct)
    {
        var rubric = await _db.InterviewRubrics.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (rubric is null)
        {
            return Result.Failure<InterviewRubricResponse>(Error.NotFound(
                "InterviewRubric.NotFound", $"Rubric {id} was not found."));
        }
        rubric.Title = r.Title.Trim();
        rubric.Description = r.Description?.Trim() ?? string.Empty;
        rubric.Weight = r.Weight;
        rubric.DisplayOrder = r.DisplayOrder;
        rubric.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);
        return Result.Success(InterviewRubricResponse.From(rubric));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var rubric = await _db.InterviewRubrics.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (rubric is null)
        {
            return Result.Failure(Error.NotFound(
                "InterviewRubric.NotFound", $"Rubric {id} was not found."));
        }
        _db.InterviewRubrics.Remove(rubric);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
