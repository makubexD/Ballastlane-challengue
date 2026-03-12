using BallastLane.Domain.Common;

namespace BallastLane.Application.CQRS;

public interface IQueryDispatcher
{
    Task<Result<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;
}
