using CareerPlatform.Api.Features.PracticeBanks.Domain;
using CareerPlatform.Api.Features.PracticeBanks.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.PracticeBanks.Service;

internal sealed class PracticeBankService : IPracticeBankService
{
    private readonly AppDbContext _db;
    public PracticeBankService(AppDbContext db) => _db = db;

    public Task<Result<IReadOnlyList<PracticeBankResponse>>> ListPublishedAsync(CancellationToken ct)
        => ListInternalAsync(publishedOnly: true, ct);

    public Task<Result<IReadOnlyList<PracticeBankResponse>>> ListAllAsync(CancellationToken ct)
        => ListInternalAsync(publishedOnly: false, ct);

    /// <summary>
    /// Shared list implementation. Question counts are aggregated in one grouped query rather than
    /// N+1 per bank.
    /// </summary>
    private async Task<Result<IReadOnlyList<PracticeBankResponse>>> ListInternalAsync(
        bool publishedOnly, CancellationToken ct)
    {
        var q = _db.PracticeQuestionBanks.AsNoTracking();
        if (publishedOnly) q = q.Where(b => b.IsPublished);
        var banks = await q
            .OrderBy(b => b.Name)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);

        if (banks.Count == 0)
        {
            return Result.Success((IReadOnlyList<PracticeBankResponse>)Array.Empty<PracticeBankResponse>());
        }

        var bankIds = banks.Select(b => b.Id).ToList();
        var counts = await _db.PracticeQuestionBankItems.AsNoTracking()
            .Where(i => bankIds.Contains(i.BankId))
            .GroupBy(i => i.BankId)
            .Select(g => new { BankId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BankId, x => x.Count, ct);

        IReadOnlyList<PracticeBankResponse> items = banks
            .Select(b => PracticeBankResponse.From(b, counts.GetValueOrDefault(b.Id, 0)))
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result<PracticeBankDetailResponse>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var s = slug.Trim().ToLowerInvariant();
        var bank = await _db.PracticeQuestionBanks.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == s && b.IsPublished, ct);
        if (bank is null)
        {
            return Result.Failure<PracticeBankDetailResponse>(Error.NotFound(
                "PracticeBank.NotFound", $"Practice bank '{s}' was not found."));
        }
        var questions = await LoadBankQuestionsAsync(bank.Id, ct);
        return Result.Success(new PracticeBankDetailResponse(
            bank.Id, bank.Slug, bank.Name, bank.Description, bank.IsPublished, questions));
    }

    public async Task<Result<PracticeBankResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var bank = await _db.PracticeQuestionBanks.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bank is null)
            return Result.Failure<PracticeBankResponse>(Error.NotFound(
                "PracticeBank.NotFound", $"Practice bank {id} was not found."));
        var count = await _db.PracticeQuestionBankItems.CountAsync(i => i.BankId == id, ct);
        return Result.Success(PracticeBankResponse.From(bank, count));
    }

    public async Task<Result<PracticeBankResponse>> CreateAsync(CreatePracticeBankRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim().ToLowerInvariant();
        if (await _db.PracticeQuestionBanks.AnyAsync(b => b.Slug == slug, ct))
        {
            return Result.Failure<PracticeBankResponse>(Error.Validation(
                "PracticeBank.SlugExists", $"A practice bank with slug '{slug}' already exists."));
        }
        var bank = new PracticeQuestionBank
        {
            Slug = slug,
            Name = r.Name.Trim(),
            Description = r.Description?.Trim(),
            IsPublished = r.IsPublished,
        };
        _db.PracticeQuestionBanks.Add(bank);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PracticeBankResponse.From(bank, 0));
    }

    public async Task<Result<PracticeBankResponse>> UpdateAsync(int id, UpdatePracticeBankRequest r, CancellationToken ct)
    {
        var bank = await _db.PracticeQuestionBanks.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bank is null)
        {
            return Result.Failure<PracticeBankResponse>(Error.NotFound(
                "PracticeBank.NotFound", $"Practice bank {id} was not found."));
        }
        bank.Name = r.Name.Trim();
        bank.Description = r.Description?.Trim();
        bank.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);

        var count = await _db.PracticeQuestionBankItems.CountAsync(i => i.BankId == id, ct);
        return Result.Success(PracticeBankResponse.From(bank, count));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var bank = await _db.PracticeQuestionBanks.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bank is null)
        {
            return Result.Failure(Error.NotFound(
                "PracticeBank.NotFound", $"Practice bank {id} was not found."));
        }
        // Remove the join rows first — no FK cascade is configured, so orphaned membership rows
        // would otherwise survive the bank they belong to.
        var items = await _db.PracticeQuestionBankItems.Where(i => i.BankId == id).ToListAsync(ct);
        _db.PracticeQuestionBankItems.RemoveRange(items);
        _db.PracticeQuestionBanks.Remove(bank);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<PracticeBankDetailResponse>> SetQuestionsAsync(
        int bankId, SetBankQuestionsRequest r, CancellationToken ct)
    {
        var bank = await _db.PracticeQuestionBanks.FirstOrDefaultAsync(b => b.Id == bankId, ct);
        if (bank is null)
        {
            return Result.Failure<PracticeBankDetailResponse>(Error.NotFound(
                "PracticeBank.NotFound", $"Practice bank {bankId} was not found."));
        }

        // Validate every incoming id actually exists before touching membership — a partial write
        // would silently drop the unknown ids and leave the caller thinking they were added.
        var existingQuestionIds = await _db.PracticeQuestions.AsNoTracking()
            .Where(q => r.QuestionIds.Contains(q.Id))
            .Select(q => q.Id)
            .ToListAsync(ct);
        var unknown = r.QuestionIds.Except(existingQuestionIds).ToList();
        if (unknown.Count > 0)
        {
            return Result.Failure<PracticeBankDetailResponse>(Error.Validation(
                "PracticeBank.UnknownQuestions",
                $"These practice-question ids do not exist: {string.Join(", ", unknown)}."));
        }

        var current = await _db.PracticeQuestionBankItems
            .Where(i => i.BankId == bankId).ToListAsync(ct);
        _db.PracticeQuestionBankItems.RemoveRange(current);

        for (var i = 0; i < r.QuestionIds.Count; i++)
        {
            _db.PracticeQuestionBankItems.Add(new PracticeQuestionBankItem
            {
                BankId = bankId,
                QuestionId = r.QuestionIds[i],
                OrderIndex = i,
            });
        }
        await _db.SaveChangesAsync(ct);

        var questions = await LoadBankQuestionsAsync(bankId, ct);
        return Result.Success(new PracticeBankDetailResponse(
            bank.Id, bank.Slug, bank.Name, bank.Description, bank.IsPublished, questions));
    }

    /// <summary>
    /// Joins membership rows to the question catalog in a single query and returns them in
    /// bank-defined order. Shared by the public detail read and the admin set-questions response.
    /// </summary>
    private async Task<IReadOnlyList<PracticeBankQuestionResponse>> LoadBankQuestionsAsync(
        int bankId, CancellationToken ct)
    {
        return await (
            from item in _db.PracticeQuestionBankItems.AsNoTracking()
            join question in _db.PracticeQuestions.AsNoTracking()
                on item.QuestionId equals question.Id
            where item.BankId == bankId
            orderby item.OrderIndex
            select new PracticeBankQuestionResponse(
                question.Id, question.Slug, question.Title,
                question.Difficulty, question.Category, item.OrderIndex)
        ).ToListAsync(ct);
    }
}
