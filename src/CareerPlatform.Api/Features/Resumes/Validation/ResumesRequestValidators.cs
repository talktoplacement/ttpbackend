using CareerPlatform.Api.Features.Resumes.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Resumes.Validation;

public sealed class CreateMyResumeRequestValidator : AbstractValidator<CreateMyResumeRequest>
{
    public CreateMyResumeRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.TemplateCode).NotEmpty().MaximumLength(64);
        RuleFor(c => c.StorageKey).MaximumLength(500);
    }
}

public sealed class UpdateMyResumeRequestValidator : AbstractValidator<UpdateMyResumeRequest>
{
    public UpdateMyResumeRequestValidator()
    {
        RuleFor(c => c.Title).MaximumLength(200);
        RuleFor(c => c.TemplateCode).MaximumLength(64);
        RuleFor(c => c.StorageKey).MaximumLength(500);
    }
}

public sealed class CreateResumeTemplateRequestValidator : AbstractValidator<CreateResumeTemplateRequest>
{
    public CreateResumeTemplateRequestValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(64)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Code must be kebab-case.");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.PreviewUrl).MaximumLength(500);
    }
}

public sealed class UpdateResumeTemplateRequestValidator : AbstractValidator<UpdateResumeTemplateRequest>
{
    public UpdateResumeTemplateRequestValidator()
    {
        RuleFor(c => c.Code).MaximumLength(64)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Code))
            .WithMessage("Code must be kebab-case.");
        RuleFor(c => c.Name).MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.PreviewUrl).MaximumLength(500);
    }
}

public sealed class AssignStudentResumeMentorRequestValidator : AbstractValidator<AssignStudentResumeMentorRequest>
{
    public AssignStudentResumeMentorRequestValidator()
    {
        // MentorUserId may be either the auth subject (Supabase GUID, up to 64 chars) or the
        // mentor's login email. 200 chars covers both comfortably.
        RuleFor(c => c.MentorUserId).MaximumLength(200)
            .When(c => !string.IsNullOrWhiteSpace(c.MentorUserId));
    }
}
