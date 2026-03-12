using BallastLane.Application.DTOs;
using BallastLane.Application.Validators;
using BallastLane.Domain.ValueObjects;
using BallastLane.Tests.TestData;

namespace BallastLane.Tests.Application;

public sealed class ValidatorTests
{
    // ── RegisterRequestValidator ────────────────────────────────────────────

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenRegisterRequestIsValid()
    {
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var errors = sut.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenEmailIsInvalid()
    {
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest("not-an-email", TestConstants.ValidPassword);

        var errors = sut.Validate(request);

        Assert.Contains(errors, e => e.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordIsTooShort()
    {
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, "Ab1xyzA"); // 7 chars

        var errors = sut.Validate(request);

        Assert.Contains(errors, e => e.Contains("8 characters"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordHasNoUppercase()
    {
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, "lowercase1");

        var errors = sut.Validate(request);

        Assert.Contains(errors, e => e.Contains("uppercase"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordHasNoDigit()
    {
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, "NoNumbers!");

        var errors = sut.Validate(request);

        Assert.Contains(errors, e => e.Contains("numeric"));
    }

    // ── LoginRequestValidator ───────────────────────────────────────────────

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenLoginRequestIsValid()
    {
        var sut = new LoginRequestValidator();
        var request = new LoginRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var errors = sut.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenEmailIsEmpty()
    {
        var sut = new LoginRequestValidator();
        var request = new LoginRequest(string.Empty, TestConstants.ValidPassword);

        var errors = sut.Validate(request);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordIsEmpty()
    {
        var sut = new LoginRequestValidator();
        var request = new LoginRequest(TestConstants.ValidEmail, string.Empty);

        var errors = sut.Validate(request);

        Assert.NotEmpty(errors);
    }

    // ── TaskRequestValidator ────────────────────────────────────────────────

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenCreateRequestIsValid()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest();

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleIsEmpty()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(title: string.Empty);

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Contains(errors, e => e.Contains("Title is required"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleExceedsMaxLength()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(title: new string('A', 201));

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Contains(errors, e => e.Contains("200"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDescriptionIsEmpty()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(description: string.Empty);

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Contains(errors, e => e.Contains("Description is required"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDueDateIsToday()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(dueDate: TestConstants.FixedUtcNow.AddHours(3));

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Contains(errors, e => e.Contains("Due date must be in the future"));
    }

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenUpdateRequestIsValid()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidUpdateRequest();

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleIsEmpty_Update()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidUpdateRequest(title: string.Empty);

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Contains(errors, e => e.Contains("Title is required"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDueDateIsToday_Update()
    {
        var sut = new TaskRequestValidator();
        var request = TestDataBuilder.ValidUpdateRequest(dueDate: TestConstants.FixedUtcNow.AddHours(3));

        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        Assert.Contains(errors, e => e.Contains("Due date must be in the future"));
    }
}
