using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Domain.Interfaces;

public interface IUserRepository
{
    Task<Result> SaveAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
