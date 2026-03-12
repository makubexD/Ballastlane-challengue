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
        // Arrange
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenEmailIsInvalid()
    {
        // Arrange
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest("not-an-email", TestConstants.ValidPassword);

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.Contains(errors, e => e.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordIsTooShort()
    {
        // Arrange
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, "Ab1xyzA"); // 7 chars

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.Contains(errors, e => e.Contains("8 characters"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordHasNoUppercase()
    {
        // Arrange
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, "lowercase1");

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.Contains(errors, e => e.Contains("uppercase"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordHasNoDigit()
    {
        // Arrange
        var sut = new RegisterRequestValidator();
        var request = new RegisterRequest(TestConstants.ValidEmail, "NoNumbers!");

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.Contains(errors, e => e.Contains("numeric"));
    }

    // ── LoginRequestValidator ───────────────────────────────────────────────

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenLoginRequestIsValid()
    {
        // Arrange
        var sut = new LoginRequestValidator();
        var request = new LoginRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenEmailIsEmpty()
    {
        // Arrange
        var sut = new LoginRequestValidator();
        var request = new LoginRequest(string.Empty, TestConstants.ValidPassword);

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPasswordIsEmpty()
    {
        // Arrange
        var sut = new LoginRequestValidator();
        var request = new LoginRequest(TestConstants.ValidEmail, string.Empty);

        // Act
        var errors = sut.Validate(request);

        // Assert
        Assert.NotEmpty(errors);
    }

    // ── CreateTaskRequestValidator ──────────────────────────────────────────

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenCreateRequestIsValid()
    {
        // Arrange
        var sut = new CreateTaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest();

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleIsEmpty()
    {
        // Arrange
        var sut = new CreateTaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(title: string.Empty);

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Contains(errors, e => e.Contains("Title is required"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var sut = new CreateTaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(title: new string('A', 201));

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Contains(errors, e => e.Contains("200"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDescriptionIsEmpty()
    {
        // Arrange
        var sut = new CreateTaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(description: string.Empty);

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Contains(errors, e => e.Contains("Description is required"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDueDateIsToday()
    {
        // Arrange
        var sut = new CreateTaskRequestValidator();
        var request = TestDataBuilder.ValidCreateRequest(dueDate: TestConstants.FixedUtcNow.AddHours(3));

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Contains(errors, e => e.Contains("Due date must be in the future"));
    }

    // ── UpdateTaskRequestValidator ──────────────────────────────────────────

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenUpdateRequestIsValid()
    {
        // Arrange
        var sut = new UpdateTaskRequestValidator();
        var request = TestDataBuilder.ValidUpdateRequest();

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenTitleIsEmpty_Update()
    {
        // Arrange
        var sut = new UpdateTaskRequestValidator();
        var request = TestDataBuilder.ValidUpdateRequest(title: string.Empty);

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Contains(errors, e => e.Contains("Title is required"));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDueDateIsToday_Update()
    {
        // Arrange
        var sut = new UpdateTaskRequestValidator();
        var request = TestDataBuilder.ValidUpdateRequest(dueDate: TestConstants.FixedUtcNow.AddHours(3));

        // Act
        var errors = sut.Validate(request, TestConstants.FixedUtcNow);

        // Assert
        Assert.Contains(errors, e => e.Contains("Due date must be in the future"));
    }
}
