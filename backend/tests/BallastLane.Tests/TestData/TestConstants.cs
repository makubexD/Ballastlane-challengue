using BallastLane.Domain.ValueObjects;

namespace BallastLane.Tests.TestData;

internal static class TestConstants
{
    internal static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    internal static readonly Guid ValidTaskId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    internal static readonly Guid UnknownTaskId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    internal static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    internal static readonly DateTime FutureDueDate = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    internal static readonly DateTime PastDueDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    internal const string ValidTitle = "Fix authentication bug";
    internal const string ValidDescription = "Session token expires too early on mobile devices.";
    internal const string LongTitle =
        "A very long title that exceeds the two hundred character maximum limit for task titles " +
        "in the BallastLane application system, which should cause a validation failure error " +
        "to be returned by the TaskValidator class immediately.";
    internal static readonly TaskItemStatus DefaultStatus = TaskItemStatus.Todo;
}
