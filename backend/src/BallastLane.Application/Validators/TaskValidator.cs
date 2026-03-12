using BallastLane.Application.DTOs;
using FluentValidation;
using FluentValidation.Results;

namespace BallastLane.Application.Validators;

/// <summary>
/// Validates <see cref="TaskRequest"/>.
/// Due date must be a future calendar day relative to the provided UTC instant.
/// </summary>
public sealed class TaskRequestValidator : AbstractValidator<TaskRequest>
{
    private const int TitleMaxLength = 200;
    private const string UtcNowKey = "utcNow";

    public TaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(TitleMaxLength)
            .WithMessage($"Title must not exceed {TitleMaxLength} characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.");

        RuleFor(x => x.DueDate)
            .Must((_, dueDate, ctx) =>
            {
                var utcNow = (DateTime)ctx.RootContextData[UtcNowKey];
                return dueDate.Date > utcNow.Date;
            })
            .WithMessage("Due date must be in the future.");
    }

    public IReadOnlyList<string> Validate(TaskRequest request, DateTime utcNow)
    {
        var context = new ValidationContext<TaskRequest>(request);
        context.RootContextData[UtcNowKey] = utcNow;
        ValidationResult result = base.Validate(context);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }
}

/// <summary>
/// Wraps <see cref="TaskRequestValidator"/> to preserve the service-layer contract
/// used by <see cref="BallastLane.Application.Services.TaskService"/>.
/// </summary>
public sealed class TaskValidator
{
    private readonly TaskRequestValidator _validator = new();

    public IReadOnlyList<string> Validate(TaskRequest request, DateTime utcNow) =>
        _validator.Validate(request, utcNow);
}
