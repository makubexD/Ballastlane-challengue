namespace BallastLane.Domain.Entities;

public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static User Create(Guid id, string email, string passwordHash, DateTime createdAt) => new()
    {
        Id = id,
        Email = email.ToLowerInvariant(),
        PasswordHash = passwordHash,
        CreatedAt = createdAt
    };
}
