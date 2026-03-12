using BallastLane.Domain.ValueObjects;

namespace BallastLane.Application.DTOs;

public sealed record CreateTaskRequest(
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTime DueDate);
