using BallastLane.Domain.ValueObjects;

namespace BallastLane.Tests.TestData;

public static class TestConstants
{
    public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ValidTaskId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid UnknownTaskId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime FutureDueDate = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime PastDueDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public const string ValidTitle = "Fix authentication bug";
    public const string ValidDescription = "Session token expires too early on mobile devices.";
    public const string LongTitle =
        "A very long title that exceeds the two hundred character maximum limit for task titles " +
        "in the BallastLane application system, which should cause a validation failure error " +
        "to be returned by the TaskValidator class immediately.";
    public static readonly TaskItemStatus DefaultStatus = TaskItemStatus.Todo;
    public const string ValidEmail = "user@example.com";
    public const string ValidPassword = "SecurePass1";
    public const string HashedPassword = "$2a$12$hashedpasswordvalue";
    public const string MockJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock";
    public const string InvalidCredentialsMessage = "Invalid email or password.";
    public const string NotFoundError = "not found";
    public const string IntegrationDbConnectionString =
        "Host=localhost;Port=5433;Database=ballastlane_test;Username=ballastlane_test;Password=ballastlane_test";
}
