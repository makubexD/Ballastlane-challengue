using BallastLane.Application.CQRS;
using BallastLane.Domain.Entities;
using BallastLane.Domain.ValueObjects;

namespace BallastLane.Application.Tasks.Commands;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTime DueDate,
    Guid UserId) : ICommand<TaskItem>;
