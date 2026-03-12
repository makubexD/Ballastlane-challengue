using BallastLane.Tests.TestData;

namespace BallastLane.Tests.Domain;

public sealed class TaskItemSoftDeleteTests
{
    [Fact]
    public void TaskItem_ShouldUseProvidedCreatedAt_NotUtcNow()
    {
        var task = TestDataBuilder.ValidTask();

        Assert.Equal(TestConstants.FixedUtcNow, task.CreatedAt);
        Assert.Equal(TestConstants.FixedUtcNow, task.UpdatedAt);
    }

    [Fact]
    public void TaskItem_ShouldUseProvidedUpdatedAt_WhenSupplied()
    {
        var updatedAt = TestConstants.FixedUtcNow.AddDays(1);
        var task = TestDataBuilder.ValidTask();
        // UpdatedAt is set via the optional parameter on Create
        var taskWithUpdatedAt = global::BallastLane.Domain.Entities.TaskItem.Create(
            TestConstants.ValidTaskId,
            TestConstants.ValidTitle,
            TestConstants.ValidDescription,
            TestConstants.DefaultStatus,
            TestConstants.FutureDueDate,
            TestConstants.ValidUserId,
            TestConstants.FixedUtcNow,
            updatedAt);

        Assert.Equal(TestConstants.FixedUtcNow, taskWithUpdatedAt.CreatedAt);
        Assert.Equal(updatedAt, taskWithUpdatedAt.UpdatedAt);
    }


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
