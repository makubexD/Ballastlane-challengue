using BallastLane.Tests.TestData;

namespace BallastLane.Tests.Domain;

public sealed class TaskItemSoftDeleteTests
{
    [Fact]
    public void IsDeleted_ShouldBeFalse_WhenNotDeleted()
    {
        var task = TestDataBuilder.ValidTask();

        Assert.False(task.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldSetDeletedAt()
    {
        var task = TestDataBuilder.ValidTask();
        var deletedAt = new DateTime(2026, 3, 13, 10, 0, 0, DateTimeKind.Utc);

        task.Delete(deletedAt);

        Assert.Equal(deletedAt, task.DeletedAt);
    }

    [Fact]
    public void IsDeleted_ShouldBeTrue_AfterDelete()
    {
        var task = TestDataBuilder.ValidTask();

        task.Delete(DateTime.UtcNow);

        Assert.True(task.IsDeleted);
    }
}
