using BallastLane.Application.DTOs;
using FluentValidation;
using FluentValidation.Results;

namespace BallastLane.Application.Validators;

/// <summary>
/// Validates <see cref="CreateTaskRequest"/>.
/// Due date must be a future calendar day relative to the provided UTC instant.
/// </summary>
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    private const int TitleMaxLength = 200;
    private const string UtcNowKey = "utcNow";

    public CreateTaskRequestValidator()
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

    public IReadOnlyList<string> Validate(CreateTaskRequest request, DateTime utcNow)
    {
        var context = new ValidationContext<CreateTaskRequest>(request);
        context.RootContextData[UtcNowKey] = utcNow;
        ValidationResult result = base.Validate(context);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }
}

/// <summary>
/// Validates <see cref="UpdateTaskRequest"/>.
/// Due date must be a future calendar day relative to the provided UTC instant.
/// </summary>
public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    private const int TitleMaxLength = 200;
    private const string UtcNowKey = "utcNow";

    public UpdateTaskRequestValidator()
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

    public IReadOnlyList<string> Validate(UpdateTaskRequest request, DateTime utcNow)
    {
        var context = new ValidationContext<UpdateTaskRequest>(request);
        context.RootContextData[UtcNowKey] = utcNow;
        ValidationResult result = base.Validate(context);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }
}

/// <summary>
/// Wraps <see cref="CreateTaskRequestValidator"/> and <see cref="UpdateTaskRequestValidator"/>
/// to preserve the service-layer contract used by <see cref="BallastLane.Application.Services.TaskService"/>.
/// </summary>
public sealed class TaskValidator
{
    private readonly CreateTaskRequestValidator _createValidator = new();
    private readonly UpdateTaskRequestValidator _updateValidator = new();

    public IReadOnlyList<string> Validate(CreateTaskRequest request, DateTime utcNow) =>
        _createValidator.Validate(request, utcNow);

    public IReadOnlyList<string> Validate(UpdateTaskRequest request, DateTime utcNow) =>
        _updateValidator.Validate(request, utcNow);
}
