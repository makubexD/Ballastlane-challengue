using BallastLane.Domain.Common;
using BallastLane.Domain.ValueObjects;

namespace BallastLane.Tests.Domain;

public sealed class TaskTitleTests
{
    [Fact]
    public void Create_ShouldReturnSuccess_WhenTitleIsValid()
    {
        var result = TaskTitle.Create("Fix authentication bug");

        Assert.True(result.IsSuccess);
        Assert.Equal("Fix authentication bug", result.Value.Value);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace_WhenTitleHasLeadingOrTrailingSpaces()
    {
        var result = TaskTitle.Create("  Fix auth bug  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Fix auth bug", result.Value.Value);
    }

    [Fact]
    public void Create_ShouldFail_WhenTitleIsNull()
    {
        var result = TaskTitle.Create(null);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldFail_WhenTitleIsEmptyOrWhitespace(string raw)
    {
        var result = TaskTitle.Create(raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_ShouldReturnSuccess_WhenTitleIsExactlyMaxLength()
    {
        var title = new string('A', TaskTitle.MaxLength);

        var result = TaskTitle.Create(title);

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskTitle.MaxLength, result.Value.Value.Length);
    }

    [Fact]
    public void Create_ShouldFail_WhenTitleExceedsMaxLength()
    {
        var title = new string('A', TaskTitle.MaxLength + 1);

        var result = TaskTitle.Create(title);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("200", result.Errors[0]);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenTitleWithSpacesTrimmedFitsMaxLength()
    {
        var title = "  " + new string('A', TaskTitle.MaxLength) + "  ";

        var result = TaskTitle.Create(title);

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskTitle.MaxLength, result.Value.Value.Length);
    }

    [Fact]
    public void ImplicitOperator_ShouldReturnValue_WhenConvertedToString()
    {
        var result = TaskTitle.Create("My Task");
        string value = result.Value;

        Assert.Equal("My Task", value);
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenBothTitlesHaveSameValue()
    {
        var first = TaskTitle.Create("My Task").Value;
        var second = TaskTitle.Create("My Task").Value;

        Assert.Equal(first, second);
        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenTitlesAreDifferent()
    {
        var first = TaskTitle.Create("Task A").Value;
        var second = TaskTitle.Create("Task B").Value;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetHashCode_ShouldBeEqual_WhenBothTitlesHaveSameValue()
    {
        var first = TaskTitle.Create("My Task").Value;
        var second = TaskTitle.Create("My Task").Value;

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var title = TaskTitle.Create("My Task").Value;

        Assert.Equal("My Task", title.ToString());
    }
}
