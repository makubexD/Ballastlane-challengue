using BallastLane.Application.DTOs;

namespace BallastLane.Application.Validators;

public sealed class TaskValidator
{
    private const int TitleMaxLength = 200;
    private const int DescriptionMaxLength = 1000;

    public IReadOnlyList<string> Validate(CreateTaskRequest request, DateTime utcNow)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Title is required.");
        else if (request.Title.Length > TitleMaxLength)
            errors.Add($"Title must not exceed {TitleMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(request.Description))
            errors.Add("Description is required.");

        if (request.DueDate.Date <= utcNow.Date)
            errors.Add("Due date must be in the future.");

        return errors;
    }

    public IReadOnlyList<string> Validate(UpdateTaskRequest request, DateTime utcNow)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Title is required.");
        else if (request.Title.Length > TitleMaxLength)
            errors.Add($"Title must not exceed {TitleMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(request.Description))
            errors.Add("Description is required.");

        if (request.DueDate.Date <= utcNow.Date)
            errors.Add("Due date must be in the future.");

        return errors;
    }
}
