using BallastLane.Application.Common;
using BallastLane.Application.CQRS;
using BallastLane.Application.Services;
using BallastLane.Application.Tasks.Queries;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Tasks.Handlers;

public sealed class GetAllTasksQueryHandler(ITaskQueryService queryService)
    : IQueryHandler<GetAllTasksQuery, PagedResult<TaskItem>>
{
    public Task<Result<PagedResult<TaskItem>>> HandleAsync(GetAllTasksQuery query, CancellationToken cancellationToken = default)
        => queryService.GetAllTasksAsync(query.UserId, query.Page, query.PageSize, cancellationToken);
}
