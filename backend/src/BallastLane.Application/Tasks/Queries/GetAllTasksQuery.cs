using BallastLane.Application.CQRS;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Tasks.Queries;

public sealed record GetAllTasksQuery(Guid UserId) : IQuery<IReadOnlyList<TaskItem>>;
