using BallastLane.Domain.ValueObjects;

namespace BallastLane.Domain.Entities;

public sealed class TaskItem
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskItemStatus Status { get; init; }
    public DateTime DueDate { get; init; }
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public static TaskItem Create(
        Guid id,
        string title,
        string description,
        TaskItemStatus status,
        DateTime dueDate,
        Guid userId,
        DateTime createdAt,
        DateTime? updatedAt = null) => new()
    {
        Id = id,
        Title = title,
        Description = description,
        Status = status,
        DueDate = dueDate,
        UserId = userId,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt ?? createdAt
    };

    public void Delete(DateTime deletedAt) => DeletedAt = deletedAt;
}
