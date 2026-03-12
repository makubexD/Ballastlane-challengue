using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;

namespace BallastLane.Infrastructure.Persistence;

public sealed class SqlUserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

    public Task<Result> SaveAsync(User user, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 6.");

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 6.");

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 6.");
}
