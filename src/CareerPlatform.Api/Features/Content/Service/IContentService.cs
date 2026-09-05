using CareerPlatform.Api.Features.Content.Dto;

namespace CareerPlatform.Api.Features.Content.Service;

public interface IContentService
{
    // Languages
    Task<Result<IReadOnlyList<LanguageResponse>>> GetAllLanguagesAsync(CancellationToken ct);
    Task<Result<LanguageResponse>> GetLanguageByIdAsync(int id, CancellationToken ct);
    Task<Result<LanguageResponse>> CreateLanguageAsync(CreateLanguageRequest body, CancellationToken ct);
    Task<Result<LanguageResponse>> UpdateLanguageAsync(int id, UpdateLanguageRequest body, CancellationToken ct);
    Task<Result> SetLanguagePublishedAsync(int id, bool isPublished, CancellationToken ct);
    Task<Result> UpdateLanguagePriceAsync(int id, decimal price, CancellationToken ct);

    // Sections
    Task<Result<int>> CreateSectionAsync(CreateSectionRequest body, CancellationToken ct);
    Task<Result> ReorderSectionsAsync(int languageId, IReadOnlyList<int> orderedIds, CancellationToken ct);

    // Topics
    Task<Result<int>> CreateTopicAsync(CreateTopicRequest body, CancellationToken ct);
    Task<Result> UpdateTopicAsync(int id, UpdateTopicRequest body, CancellationToken ct);
    Task<Result> DeleteTopicAsync(int id, CancellationToken ct);
    Task<Result> ReorderTopicsAsync(int sectionId, IReadOnlyList<int> orderedIds, CancellationToken ct);
    Task<Result<TopicDetailResponse>> GetTopicByIdAsync(int id, CancellationToken ct);

    // Public curriculum
    Task<Result<CurriculumResponse>> GetPublicCurriculumAsync(string slug, CancellationToken ct);

    // Excel import
    Task<Result<ImportInterviewQuestionsResponse>> ImportInterviewQuestionsAsync(
        byte[] fileBytes, string fileName, CancellationToken ct);
}
