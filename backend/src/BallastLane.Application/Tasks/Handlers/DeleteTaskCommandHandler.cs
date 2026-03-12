using BallastLane.Application.CQRS;
using BallastLane.Application.Services;
using BallastLane.Application.Tasks.Commands;
using BallastLane.Domain.Common;

namespace BallastLane.Application.Tasks.Handlers;

public sealed class DeleteTaskCommandHandler(ITaskCommandService commandService)
    : ICommandHandler<DeleteTaskCommand>
{
    public Task<Result> HandleAsync(DeleteTaskCommand command, CancellationToken cancellationToken = default)
        => commandService.DeleteTaskAsync(command.TaskId, command.UserId, cancellationToken);
}
