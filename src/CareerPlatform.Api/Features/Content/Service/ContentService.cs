using System.Text.RegularExpressions;
using CareerPlatform.Api.Features.Content.Domain;
using CareerPlatform.Api.Features.Content.Dto;
using CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;
using CareerPlatform.Api.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Content.Service;

/// <summary>Content-management workflow. Ports the 13 legacy MediatR handlers into service methods.</summary>
internal sealed partial class ContentService : IContentService
{
    private const string InterviewSlugPrefix = "interview-";

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public ContentService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ── Languages ───────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<LanguageResponse>>> GetAllLanguagesAsync(CancellationToken ct)
    {
        var languages = await _db.Languages
            .OrderBy(l => l.Title)
            .Select(l => new LanguageResponse(
                l.Id, l.Title, l.Slug, l.Description, l.IsPublished, l.Price, l.LastUpdated))
            .ToListAsync(ct);
        IReadOnlyList<LanguageResponse> items = languages;
        return Result.Success(items);
    }

    public async Task<Result<LanguageResponse>> GetLanguageByIdAsync(int id, CancellationToken ct)
    {
        var l = await _db.Languages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l is null)
        {
            return Result.Failure<LanguageResponse>(Error.NotFound(
                "Language.NotFound", $"Language {id} was not found."));
        }
        return Result.Success(ToResponse(l));
    }

    public async Task<Result<LanguageResponse>> CreateLanguageAsync(CreateLanguageRequest body, CancellationToken ct)
    {
        var slug = body.Slug.ToLowerInvariant().Trim();
        if (await _db.Languages.AnyAsync(l => l.Slug == slug, ct))
        {
            return Result.Failure<LanguageResponse>(Error.Validation(
                "Language.SlugExists", $"A curriculum track with slug '{slug}' already exists."));
        }
        var language = new Language
        {
            Title = body.Title.Trim(),
            Slug = slug,
            Description = body.Description,
            IsPublished = body.IsPublished,
            Price = body.Price,
            LastUpdated = DateTime.UtcNow,
        };
        _db.Languages.Add(language);
        await _db.SaveChangesAsync(ct);
        return Result.Success(ToResponse(language));
    }

    public async Task<Result<LanguageResponse>> UpdateLanguageAsync(int id, UpdateLanguageRequest body, CancellationToken ct)
    {
        var language = await _db.Languages.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (language is null)
        {
            return Result.Failure<LanguageResponse>(Error.NotFound(
                "Language.NotFound", $"Language {id} was not found."));
        }
        if (body.Slug is not null)
        {
            var slug = body.Slug.ToLowerInvariant().Trim();
            if (slug != language.Slug)
            {
                if (await _db.Languages.AnyAsync(l => l.Slug == slug && l.Id != id, ct))
                {
                    return Result.Failure<LanguageResponse>(Error.Validation(
                        "Language.SlugExists", $"A different curriculum track already uses slug '{slug}'."));
                }
                language.Slug = slug;
            }
        }
        if (body.Title is not null) language.Title = body.Title.Trim();
        if (body.Description is not null) language.Description = body.Description;
        if (body.IsPublished is not null) language.IsPublished = body.IsPublished.Value;
        if (body.Price is not null) language.Price = body.Price.Value;
        language.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success(ToResponse(language));
    }

    private static LanguageResponse ToResponse(Language l) => new(
        l.Id, l.Title, l.Slug, l.Description, l.IsPublished, l.Price, l.LastUpdated);

    public async Task<Result> SetLanguagePublishedAsync(int id, bool isPublished, CancellationToken ct)
    {
        var language = await _db.Languages.FindAsync(new object[] { id }, ct);
        if (language is null)
        {
            return Result.Failure(Error.NotFound("Language.NotFound", $"Language {id} was not found."));
        }
        language.IsPublished = isPublished;
        language.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UpdateLanguagePriceAsync(int id, decimal price, CancellationToken ct)
    {
        var language = await _db.Languages.FindAsync(new object[] { id }, ct);
        if (language is null)
        {
            return Result.Failure(Error.NotFound("Language.NotFound", $"Language {id} was not found."));
        }
        language.Price = price;
        language.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Sections ────────────────────────────────────────────────────────────

    public async Task<Result<int>> CreateSectionAsync(CreateSectionRequest body, CancellationToken ct)
    {
        var section = new Section
        {
            LanguageId = body.LanguageId,
            Title = body.Title,
            OrderIndex = body.OrderIndex,
        };
        _db.Sections.Add(section);
        await _db.SaveChangesAsync(ct);
        return Result.Success(section.Id);
    }

    public async Task<Result> ReorderSectionsAsync(int languageId, IReadOnlyList<int> orderedIds, CancellationToken ct)
    {
        var language = await _db.Languages.FindAsync(new object[] { languageId }, ct);
        if (language is null)
        {
            return Result.Failure(Error.NotFound("Language.NotFound", $"Language {languageId} was not found."));
        }
        var sections = await _db.Sections.Where(s => s.LanguageId == languageId).ToListAsync(ct);
        var position = new Dictionary<int, int>();
        for (var i = 0; i < orderedIds.Count; i++) position[orderedIds[i]] = i;
        foreach (var section in sections)
        {
            if (position.TryGetValue(section.Id, out var index)) section.OrderIndex = index;
        }
        language.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Topics ──────────────────────────────────────────────────────────────

    public async Task<Result<int>> CreateTopicAsync(CreateTopicRequest body, CancellationToken ct)
    {
        // Only interview tracks (slug "interview-…") may charge; curriculum topics are always free.
        var isInterview = await IsInterviewSectionAsync(body.SectionId, ct);
        var topic = new Topic
        {
            SectionId = body.SectionId,
            Title = body.Title.Trim(),
            Slug = body.Slug.ToLowerInvariant().Trim(),
            Content = body.Content,
            OrderIndex = body.OrderIndex,
            CompanyTags = Normalize(body.CompanyTags),
            Frequency = Normalize(body.Frequency),
            Difficulty = Normalize(body.Difficulty),
            ReadTimeMinutes = body.ReadTimeMinutes is > 0 ? body.ReadTimeMinutes : null,
            IsPaid = isInterview && body.IsPaid,
            LastUpdatedUtc = DateTime.UtcNow,
        };
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync(ct);
        return Result.Success(topic.Id);
    }

    public async Task<Result> UpdateTopicAsync(int id, UpdateTopicRequest body, CancellationToken ct)
    {
        var topic = await _db.Topics.FindAsync(new object[] { id }, ct);
        if (topic is null)
        {
            return Result.Failure(Error.NotFound("Topic.NotFound", $"Topic {id} was not found."));
        }
        topic.Title = body.Title.Trim();
        topic.Slug = body.Slug.ToLowerInvariant().Trim();
        topic.Content = body.Content;
        topic.OrderIndex = body.OrderIndex;
        topic.CompanyTags = Normalize(body.CompanyTags);
        topic.Frequency = Normalize(body.Frequency);
        topic.Difficulty = Normalize(body.Difficulty);
        topic.ReadTimeMinutes = body.ReadTimeMinutes is > 0 ? body.ReadTimeMinutes : null;
        // Curriculum topics are always free; only interview topics honour the Paid flag.
        var isInterview = await IsInterviewSectionAsync(topic.SectionId, ct);
        topic.IsPaid = isInterview && body.IsPaid;
        topic.LastUpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>True when the section's parent language is an interview track (slug "interview-…").</summary>
    private async Task<bool> IsInterviewSectionAsync(int sectionId, CancellationToken ct)
    {
        var slug = await _db.Sections
            .Where(s => s.Id == sectionId)
            .Select(s => s.Language!.Slug)
            .FirstOrDefaultAsync(ct);
        return slug is not null && slug.StartsWith(InterviewSlugPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<Result> DeleteTopicAsync(int id, CancellationToken ct)
    {
        var topic = await _db.Topics.FindAsync(new object[] { id }, ct);
        if (topic is null)
        {
            return Result.Failure(Error.NotFound("Topic.NotFound", $"Topic {id} was not found."));
        }
        _db.Topics.Remove(topic);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReorderTopicsAsync(int sectionId, IReadOnlyList<int> orderedIds, CancellationToken ct)
    {
        var section = await _db.Sections.FindAsync(new object[] { sectionId }, ct);
        if (section is null)
        {
            return Result.Failure(Error.NotFound("Section.NotFound", $"Section {sectionId} was not found."));
        }
        var topics = await _db.Topics.Where(t => t.SectionId == sectionId).ToListAsync(ct);
        var position = new Dictionary<int, int>();
        for (var i = 0; i < orderedIds.Count; i++) position[orderedIds[i]] = i;
        foreach (var topic in topics)
        {
            if (position.TryGetValue(topic.Id, out var index)) topic.OrderIndex = index;
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<TopicDetailResponse>> GetTopicByIdAsync(int id, CancellationToken ct)
    {
        var topic = await _db.Topics
            .Include(t => t.Section)
                .ThenInclude(s => s!.Language)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (topic is null)
        {
            return Result.Failure<TopicDetailResponse>(Error.NotFound(
                "Topic.NotFound", $"Topic {id} was not found."));
        }
        return Result.Success(new TopicDetailResponse(
            topic.Id, topic.SectionId, topic.Title, topic.Slug, topic.Content, topic.OrderIndex,
            topic.Section?.Title ?? string.Empty,
            topic.Section?.Language?.Slug ?? string.Empty,
            topic.IsPaid));
    }

    // ── Public curriculum ───────────────────────────────────────────────────

    public async Task<Result<CurriculumResponse>> GetPublicCurriculumAsync(string slug, CancellationToken ct)
    {
        var normalized = slug.ToLowerInvariant().Trim();
        var language = await _db.Languages
            .Include(l => l.Sections.OrderBy(s => s.OrderIndex))
                .ThenInclude(s => s.Topics.OrderBy(t => t.OrderIndex))
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.Slug.ToLower() == normalized && l.IsPublished, ct);
        if (language is null)
        {
            return Result.Failure<CurriculumResponse>(Error.NotFound(
                "Curriculum.NotFound", $"Curriculum for '{slug}' was not found."));
        }

        // Determine paid entitlement once. Anonymous callers and free-plan students are not
        // entitled; any active paid subscription unlocks premium topics. The check is enforced
        // here on the server so a locked topic's body never leaves the API (Req: premium gating).
        var isEntitled = false;
        var userId = _currentUser.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            var plan = await EntitlementDeriver.DeriveEffectivePlanAsync(_db, userId, DateTime.UtcNow, ct);
            isEntitled = EntitlementDeriver.IsProPlan(plan);
        }

        // Only interview tracks paywall. Curriculum tracks are always free — even if a row somehow
        // carries a stale IsPaid flag, it never locks here.
        var languageIsInterview = language.Slug.StartsWith(InterviewSlugPrefix, StringComparison.OrdinalIgnoreCase);

        var response = new CurriculumResponse(
            language.Id, language.Title, language.Slug, language.Description,
            language.Sections
                .OrderBy(s => s.OrderIndex)
                .Select(s => new CurriculumSectionResponse(
                    s.Id, s.Title, s.OrderIndex,
                    s.Topics
                        .OrderBy(t => t.OrderIndex)
                        .Select(t =>
                        {
                            var paid = languageIsInterview && t.IsPaid;
                            var locked = paid && !isEntitled;
                            return new CurriculumTopicResponse(
                                t.Id, t.Title, t.Slug,
                                locked ? string.Empty : t.Content, // withhold premium body when locked
                                t.OrderIndex,
                                t.CompanyTags, t.Frequency, t.Difficulty, t.ReadTimeMinutes, t.LastUpdatedUtc,
                                paid, locked);
                        })
                        .ToList()))
                .ToList());
        return Result.Success(response);
    }

    // ── Excel import (interview questions) ──────────────────────────────────

    public async Task<Result<ImportInterviewQuestionsResponse>> ImportInterviewQuestionsAsync(
        byte[] fileBytes, string fileName, CancellationToken ct)
    {
        _ = fileName; // used only for diagnostics upstream
        List<ImportRowError> errors = new();
        int totalRows = 0, languagesCreated = 0, sectionsCreated = 0, topicsCreated = 0, topicsUpdated = 0;

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportInterviewQuestionsResponse>(Error.Validation(
                "Import.InvalidWorkbook",
                $"The uploaded file is not a valid Excel workbook: {ex.Message}"));
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null)
            {
                return Result.Failure<ImportInterviewQuestionsResponse>(Error.Validation(
                    "Import.EmptyWorkbook", "The workbook does not contain any sheets."));
            }

            var headerRow = sheet.Row(1);
            Dictionary<string, int> headers = new(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var name = (cell.GetString() ?? string.Empty)
                    .Trim()
                    .Replace(" ", string.Empty, StringComparison.Ordinal);
                if (name.Length > 0 && !headers.ContainsKey(name))
                {
                    headers[name] = cell.Address.ColumnNumber;
                }
            }

            string[] required = { "LanguageSlug", "LanguageTitle", "SectionTitle", "TopicTitle", "TopicSlug", "TopicContent" };
            foreach (var req in required)
            {
                if (!headers.ContainsKey(req))
                {
                    return Result.Failure<ImportInterviewQuestionsResponse>(Error.Validation(
                        "Import.MissingHeader",
                        $"Required column '{req}' is missing from the first sheet's header row."));
                }
            }

            Dictionary<string, Language> languageBySlug = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<(int LanguageId, string Title), Section> sectionByTitle = new();
            Dictionary<(int SectionId, string Slug), Topic> topicByKey = new();

            var lastRow = sheet.LastRowUsed();
            var lastRowNumber = lastRow?.RowNumber() ?? 1;
            for (int rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
            {
                var row = sheet.Row(rowNumber);
                if (row.IsEmpty()) continue;
                totalRows++;

                string Read(string col) =>
                    headers.TryGetValue(col, out var idx)
                        ? (row.Cell(idx).GetString() ?? string.Empty).Trim()
                        : string.Empty;
                int? ReadInt(string col)
                {
                    var raw = Read(col);
                    return int.TryParse(raw, out var n) ? n : null;
                }

                var langSlugSuffix = Read("LanguageSlug").ToLowerInvariant();
                var langTitle = Read("LanguageTitle");
                var langDescription = Read("LanguageDescription");
                var sectionTitle = Read("SectionTitle");
                var sectionOrder = ReadInt("SectionOrder") ?? 0;
                var topicTitle = Read("TopicTitle");
                var topicSlug = Read("TopicSlug").ToLowerInvariant();
                var topicContent = Read("TopicContent");
                var topicOrder = ReadInt("TopicOrder") ?? 0;
                var companyTags = Read("CompanyTags");
                var frequency = Read("Frequency");
                var difficulty = Read("Difficulty");
                var readTime = ReadInt("ReadTimeMinutes");

                if (langSlugSuffix.Length == 0 || langTitle.Length == 0 ||
                    sectionTitle.Length == 0 || topicTitle.Length == 0 ||
                    topicSlug.Length == 0 || topicContent.Length == 0)
                {
                    errors.Add(new ImportRowError(rowNumber, "One or more required fields are empty."));
                    continue;
                }
                if (!SlugPattern().IsMatch(langSlugSuffix))
                {
                    errors.Add(new ImportRowError(rowNumber,
                        $"LanguageSlug '{langSlugSuffix}' must be kebab-case (letters, digits, single dashes)."));
                    continue;
                }
                if (!SlugPattern().IsMatch(topicSlug))
                {
                    errors.Add(new ImportRowError(rowNumber,
                        $"TopicSlug '{topicSlug}' must be kebab-case (letters, digits, single dashes)."));
                    continue;
                }
                var fullLangSlug = InterviewSlugPrefix + langSlugSuffix;

                if (!languageBySlug.TryGetValue(fullLangSlug, out var language))
                {
                    language = await _db.Languages.FirstOrDefaultAsync(l => l.Slug == fullLangSlug, ct);
                    if (language is null)
                    {
                        language = new Language
                        {
                            Slug = fullLangSlug,
                            Title = langTitle,
                            Description = langDescription,
                            IsPublished = false,
                            LastUpdated = DateTime.UtcNow,
                        };
                        _db.Languages.Add(language);
                        await _db.SaveChangesAsync(ct);
                        languagesCreated++;
                    }
                    languageBySlug[fullLangSlug] = language;
                }

                var sectionKey = (language.Id, sectionTitle);
                if (!sectionByTitle.TryGetValue(sectionKey, out var section))
                {
                    section = await _db.Sections.FirstOrDefaultAsync(
                        s => s.LanguageId == language.Id && s.Title == sectionTitle, ct);
                    if (section is null)
                    {
                        section = new Section
                        {
                            LanguageId = language.Id,
                            Title = sectionTitle,
                            OrderIndex = sectionOrder,
                        };
                        _db.Sections.Add(section);
                        await _db.SaveChangesAsync(ct);
                        sectionsCreated++;
                    }
                    sectionByTitle[sectionKey] = section;
                }

                var topicKey = (section.Id, topicSlug);
                if (!topicByKey.TryGetValue(topicKey, out var topic))
                {
                    topic = await _db.Topics.FirstOrDefaultAsync(
                        t => t.SectionId == section.Id && t.Slug == topicSlug, ct);
                }
                if (topic is null)
                {
                    topic = new Topic
                    {
                        SectionId = section.Id,
                        Slug = topicSlug,
                        Title = topicTitle,
                        Content = topicContent,
                        OrderIndex = topicOrder,
                        CompanyTags = NullIfEmpty(companyTags),
                        Frequency = NullIfEmpty(frequency),
                        Difficulty = NullIfEmpty(difficulty),
                        ReadTimeMinutes = readTime is > 0 ? readTime : null,
                        LastUpdatedUtc = DateTime.UtcNow,
                    };
                    _db.Topics.Add(topic);
                    topicsCreated++;
                }
                else
                {
                    topic.Title = topicTitle;
                    topic.Content = topicContent;
                    topic.OrderIndex = topicOrder;
                    topic.CompanyTags = NullIfEmpty(companyTags);
                    topic.Frequency = NullIfEmpty(frequency);
                    topic.Difficulty = NullIfEmpty(difficulty);
                    topic.ReadTimeMinutes = readTime is > 0 ? readTime : null;
                    topic.LastUpdatedUtc = DateTime.UtcNow;
                    topicsUpdated++;
                }
                topicByKey[topicKey] = topic;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Result.Success(new ImportInterviewQuestionsResponse(
            totalRows, languagesCreated, sectionsCreated, topicsCreated, topicsUpdated, errors));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
