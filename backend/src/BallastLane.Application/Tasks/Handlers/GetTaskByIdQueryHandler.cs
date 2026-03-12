using BallastLane.Application.CQRS;
using BallastLane.Application.Services;
using BallastLane.Application.Tasks.Queries;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Tasks.Handlers;

public sealed class GetTaskByIdQueryHandler(ITaskQueryService queryService)
    : IQueryHandler<GetTaskByIdQuery, TaskItem>
{
    public Task<Result<TaskItem>> HandleAsync(GetTaskByIdQuery query, CancellationToken cancellationToken = default)
        => queryService.GetTaskByIdAsync(query.TaskId, query.UserId, cancellationToken);
}
