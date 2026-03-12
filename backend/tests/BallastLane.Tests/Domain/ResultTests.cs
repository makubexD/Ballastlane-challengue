using BallastLane.Domain.Common;

namespace BallastLane.Tests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void Result_Ok_ShouldBeSuccess_WithCorrectValue()
    {
        const string expectedValue = "test-value";

        var result = Result<string>.Ok(expectedValue);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(expectedValue, result.Value);
        Assert.Empty(result.Errors);
        Assert.Equal(ResultErrorType.None, result.ErrorType);
    }

    [Fact]
    public void Result_Fail_ShouldNotBeSuccess_WithSingleError()
    {
        const string expectedError = "Something failed";

        var result = Result<string>.Fail(expectedError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors[0]);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Result_Fail_ShouldNotBeSuccess_WithMultipleErrors()
    {
        var errors = new[] { "Error one", "Error two", "Error three" };

        var result = Result<string>.Fail(errors);

        Assert.False(result.IsSuccess);
        Assert.Equal(3, result.Errors.Count);
        Assert.Equal(errors, result.Errors);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Result_Fail_ShouldUseProvidedErrorType()
    {
        var result = Result<string>.Fail("Resource not found.", ResultErrorType.NotFound);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Theory]
    [InlineData(ResultErrorType.Validation)]
    [InlineData(ResultErrorType.NotFound)]
    [InlineData(ResultErrorType.Conflict)]
    [InlineData(ResultErrorType.Unauthorized)]
    public void Result_Fail_ShouldPreserveAllErrorTypes(ResultErrorType errorType)
    {
        var result = Result<string>.Fail("error", errorType);

        Assert.Equal(errorType, result.ErrorType);
    }

    [Fact]
    public void Result_Value_ShouldThrow_WhenResultIsFailed()
    {
        var result = Result<string>.Fail("Error");

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("failed Result", exception.Message);
    }

    [Fact]
    public void NonGenericResult_Ok_ShouldBeSuccess()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(ResultErrorType.None, result.ErrorType);
    }

    [Fact]
    public void NonGenericResult_Fail_ShouldNotBeSuccess()
    {
        const string expectedError = "Operation failed";

        var result = Result.Fail(expectedError);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal(expectedError, result.Errors[0]);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void NonGenericResult_Fail_ShouldUseProvidedErrorType()
    {
        var result = Result.Fail("Access denied.", ResultErrorType.Unauthorized);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.ErrorType);
    }
}
