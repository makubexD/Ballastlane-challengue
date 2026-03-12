using BallastLane.Application.CQRS;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Tasks.Queries;

public sealed record GetTaskByIdQuery(Guid TaskId, Guid UserId) : IQuery<TaskItem>;
