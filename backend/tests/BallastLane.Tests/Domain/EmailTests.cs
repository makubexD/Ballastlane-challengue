using BallastLane.Domain.Common;
using BallastLane.Domain.ValueObjects;

namespace BallastLane.Tests.Domain;

public sealed class EmailTests
{
    [Fact]
    public void Create_ShouldReturnSuccess_WhenEmailIsValid()
    {
        var result = Email.Create("User@Example.COM");

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Value);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace_WhenEmailHasLeadingOrTrailingSpaces()
    {
        var result = Email.Create("  user@example.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Value);
    }

    [Fact]
    public void Create_ShouldLowercaseValue_WhenEmailContainsUppercaseLetters()
    {
        var result = Email.Create("ADMIN@DOMAIN.ORG");

        Assert.True(result.IsSuccess);
        Assert.Equal("admin@domain.org", result.Value.Value);
    }

    [Fact]
    public void Create_ShouldFail_WhenEmailIsNull()
    {
        var result = Email.Create(null);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldFail_WhenEmailIsEmptyOrWhitespace(string raw)
    {
        var result = Email.Create(raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_ShouldFail_WhenEmailHasNoAtSign()
    {
        var result = Email.Create("userexample.com");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_ShouldFail_WhenAtSignIsAtPositionZero()
    {
        var result = Email.Create("@example.com");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_ShouldFail_WhenAtSignIsAtLastPosition()
    {
        var result = Email.Create("user@");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void ImplicitOperator_ShouldReturnValue_WhenConvertedToString()
    {
        var result = Email.Create("user@example.com");
        string value = result.Value;

        Assert.Equal("user@example.com", value);
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenBothEmailsHaveSameValue()
    {
        var first = Email.Create("user@example.com").Value;
        var second = Email.Create("user@example.com").Value;

        Assert.Equal(first, second);
        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenEmailsAreDifferent()
    {
        var first = Email.Create("a@example.com").Value;
        var second = Email.Create("b@example.com").Value;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetHashCode_ShouldBeEqual_WhenBothEmailsHaveSameValue()
    {
        var first = Email.Create("user@example.com").Value;
        var second = Email.Create("user@example.com").Value;

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var email = Email.Create("user@example.com").Value;

        Assert.Equal("user@example.com", email.ToString());
    }
}
