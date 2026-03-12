using BallastLane.Application.CQRS;
using BallastLane.Application.DTOs;
using BallastLane.Application.Services;
using BallastLane.Application.Tasks.Commands;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Tasks.Handlers;

public sealed class CreateTaskCommandHandler(ITaskCommandService commandService)
    : ICommandHandler<CreateTaskCommand, TaskItem>
{
    public Task<Result<TaskItem>> HandleAsync(CreateTaskCommand command, CancellationToken cancellationToken = default)
    {
        var request = new TaskRequest(command.Title, command.Description, command.Status, command.DueDate);
        return commandService.CreateTaskAsync(request, command.UserId, cancellationToken);
    }
}
