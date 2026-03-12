using BallastLane.Application.Tasks.Queries;
using BallastLane.Tests.TestData;

namespace BallastLane.Tests.Application;

public sealed class GetAllTasksQueryTests
{
    [Fact]
    public void GetAllTasksQuery_ShouldHaveDefaultPage1_WhenCreatedWithUserIdOnly()
    {
        var query = new GetAllTasksQuery(TestConstants.ValidUserId);

        Assert.Equal(1, query.Page);
    }

    [Fact]
    public void GetAllTasksQuery_ShouldHaveDefaultPageSize20_WhenCreatedWithUserIdOnly()
    {
        var query = new GetAllTasksQuery(TestConstants.ValidUserId);

        Assert.Equal(20, query.PageSize);
    }

    [Fact]
    public void GetAllTasksQuery_ShouldPreserveExplicitPageAndPageSize()
    {
        var query = new GetAllTasksQuery(TestConstants.ValidUserId, 3, 50);

        Assert.Equal(3, query.Page);
        Assert.Equal(50, query.PageSize);
        Assert.Equal(TestConstants.ValidUserId, query.UserId);
    }
}
