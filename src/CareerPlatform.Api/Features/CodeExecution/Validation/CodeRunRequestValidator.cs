using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.CodeExecution.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.CodeExecution.Validation;

/// <summary>
/// Bounds the ad-hoc run payload. Sizes come from <see cref="CodeExecutionOptions"/> so one operator
/// setting governs both this endpoint and assessment autosave — the two must not drift, or a snippet
/// that runs here could be rejected when submitted as an answer.
///
/// The language is only length-checked here; whether it is actually offered depends on runtime
/// sandbox state and is enforced in the service.
/// </summary>
public sealed class CodeRunRequestValidator : AbstractValidator<CodeRunRequest>
{
    public CodeRunRequestValidator(IOptions<CodeExecutionOptions> codeExecution)
    {
        ArgumentNullException.ThrowIfNull(codeExecution);
        var options = codeExecution.Value;

        RuleFor(r => r.Language).NotEmpty().MaximumLength(32);
        RuleFor(r => r.SourceCode).NotEmpty().MaximumLength(options.MaxSourceCodeLength);
        RuleFor(r => r.Stdin).MaximumLength(options.MaxStdinLength);
    }
}
