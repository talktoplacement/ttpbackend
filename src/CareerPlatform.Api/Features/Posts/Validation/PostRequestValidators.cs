using CareerPlatform.Api.Features.Posts.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Posts.Validation;

public sealed class PostEditorRequestValidator : AbstractValidator<PostEditorRequest>
{
    public PostEditorRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentMarkdown).NotEmpty()
            .WithMessage("Write some content before saving.");
        RuleFor(x => x.Excerpt).MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Excerpt));
        RuleFor(x => x.CoverImageUrl).MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl));
        RuleFor(x => x.Tags).MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Tags));
    }
}

public sealed class ReviewPostRequestValidator : AbstractValidator<ReviewPostRequest>
{
    private static readonly string[] AllowedDecisions = { "approve", "reject" };

    public ReviewPostRequestValidator()
    {
        RuleFor(x => x.Decision).NotEmpty()
            .Must(d => AllowedDecisions.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Decision must be 'approve' or 'reject'.");
        // A rejection must explain why so the author can fix it.
        RuleFor(x => x.Note).NotEmpty()
            .When(x => string.Equals(x.Decision, "reject", StringComparison.OrdinalIgnoreCase))
            .WithMessage("A rejection note is required so the author knows what to fix.");
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}
