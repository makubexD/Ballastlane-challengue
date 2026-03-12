using BallastLane.Application.Common;
using BallastLane.Application.CQRS;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Tasks.Queries;

public sealed record GetAllTasksQuery(Guid UserId, int Page = 1, int PageSize = 20) : IQuery<PagedResult<TaskItem>>;
