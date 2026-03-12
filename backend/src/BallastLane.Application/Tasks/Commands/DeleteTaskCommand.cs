using BallastLane.Application.CQRS;

namespace BallastLane.Application.Tasks.Commands;

public sealed record DeleteTaskCommand(Guid TaskId, Guid UserId) : ICommand;
