using CareerPlatform.Api.Features.PlacementRoles.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.PlacementRoles.Validation;

public sealed class CreatePlacementRoleRequestValidator : AbstractValidator<CreatePlacementRoleRequest>
{
    public CreatePlacementRoleRequestValidator()
    {
        RuleFor(r => r.Slug).NotEmpty().MaximumLength(64)
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.AvgCtcRange).MaximumLength(64);
        RuleFor(r => r.RequirementsMarkdown).MaximumLength(4000);
    }
}

public sealed class UpdatePlacementRoleRequestValidator : AbstractValidator<UpdatePlacementRoleRequest>
{
    public UpdatePlacementRoleRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.AvgCtcRange).MaximumLength(64);
        RuleFor(r => r.RequirementsMarkdown).MaximumLength(4000);
    }
}
