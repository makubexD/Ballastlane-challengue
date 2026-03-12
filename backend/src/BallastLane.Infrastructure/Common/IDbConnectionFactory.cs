using System.Data;

namespace BallastLane.Infrastructure.Common;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateAsync(CancellationToken cancellationToken = default);
}
