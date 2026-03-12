using BallastLane.Application.CQRS;
using BallastLane.Application.DTOs;
using BallastLane.Application.Services;
using BallastLane.Application.Tasks.Commands;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Tasks.Handlers;

public sealed class UpdateTaskCommandHandler(ITaskCommandService commandService)
    : ICommandHandler<UpdateTaskCommand, TaskItem>
{
    public Task<Result<TaskItem>> HandleAsync(UpdateTaskCommand command, CancellationToken cancellationToken = default)
    {
        var request = new TaskRequest(command.Title, command.Description, command.Status, command.DueDate);
        return commandService.UpdateTaskAsync(command.TaskId, request, command.UserId, cancellationToken);
    }
}
