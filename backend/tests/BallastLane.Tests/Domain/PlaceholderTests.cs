using BallastLane.Domain.Common;

namespace BallastLane.Tests.Domain;

public sealed class PlaceholderTests
{
    [Fact]
    public void Result_ShouldBeSuccess_WhenCreatedWithOk()
    {
        var result = Result<string>.Ok("value");
        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void Result_ShouldBeFailed_WhenCreatedWithFail()
    {
        var result = Result<string>.Fail("Something went wrong");
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Something went wrong", result.Errors[0]);
    }
}
