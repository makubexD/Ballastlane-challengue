using BallastLane.Domain.ValueObjects;

namespace BallastLane.Application.DTOs;

public sealed record TaskRequest(
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTime DueDate);
