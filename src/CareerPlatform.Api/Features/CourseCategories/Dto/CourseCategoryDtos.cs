using CareerPlatform.Api.Features.CourseCategories.Domain;

namespace CareerPlatform.Api.Features.CourseCategories.Dto;

public sealed record CourseCategoryResponse(
    int Id, string Slug, string Name, string? Description,
    int DisplayOrder, bool IsPublished)
{
    public static CourseCategoryResponse From(CourseCategory c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new CourseCategoryResponse(
            c.Id, c.Slug, c.Name, c.Description, c.DisplayOrder, c.IsPublished);
    }
}

public sealed record CreateCourseCategoryRequest(
    string Slug, string Name, string? Description,
    int DisplayOrder = 0, bool IsPublished = true);

public sealed record UpdateCourseCategoryRequest(
    string Name, string? Description,
    int DisplayOrder, bool IsPublished);
