using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CareerPlatform.Api.Common;

/// <summary>
/// Global MVC action filter that runs every registered FluentValidation validator against each
/// non-null action argument before the controller action executes. When any validator fails, the
/// filter short-circuits with an RFC 7807 <see cref="ProblemDetails"/> (400) whose <c>errors</c>
/// extension carries the per-field failures. This is the controller-side counterpart to the
/// (formerly-MediatR) validation pipeline: validators keep their existing shape
/// (<c>AbstractValidator&lt;TRequest&gt;</c> beside the DTO) and are auto-invoked by MVC.
/// </summary>
public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public FluentValidationActionFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<FieldError>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = (IValidator?)_services.GetService(validatorType);
            if (validator is null) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                {
                    if (failure is null) continue;
                    failures.Add(new FieldError(failure.PropertyName, failure.ErrorMessage));
                }
            }
        }

        if (failures.Count > 0)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation.Failed",
                Detail = "One or more validation errors occurred.",
            };
            problem.Extensions["errors"] = failures
                .GroupBy(f => f.Field)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());
            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
            return;
        }

        await next();
    }
}
