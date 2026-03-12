using BallastLane.API.Controllers;
using BallastLane.API.Services;
using BallastLane.Application.DTOs;
using BallastLane.Application.Services;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Tests.TestData;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BallastLane.Tests.API;

public sealed class AuthControllerTests
{
    private const string AlreadyExistsError = "An account with this email already exists.";
    private const string PasswordTooShortError = "Password must be at least 8 characters.";
    private const string UserNotFoundError = "not found";

    private static (AuthController sut, Mock<IAuthService> authService) BuildSut()
    {
        var authService = new Mock<IAuthService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var sut = new AuthController(authService.Object, currentUser.Object);
        return (sut, authService);
    }

    [Fact]
    public async Task Register_ShouldReturn201_WhenEmailIsUniqueAndCredentialsAreValid()
    {
        var (sut, authService) = BuildSut();
        var user = User.Create(TestConstants.ValidUserId, TestConstants.ValidEmail, "hash");
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);
        authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<User>.Ok(user));

        var result = await sut.Register(request, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, statusResult.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturn409_WhenEmailAlreadyExists()
    {
        var (sut, authService) = BuildSut();
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);
        authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<User>.Fail(AlreadyExistsError));

        var result = await sut.Register(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenPasswordIsInvalid()
    {
        var (sut, authService) = BuildSut();
        var request = new RegisterRequest(TestConstants.ValidEmail, "weak");
        authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<User>.Fail([PasswordTooShortError]));

        var result = await sut.Register(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturn200WithToken_WhenCredentialsAreValid()
    {
        var (sut, authService) = BuildSut();
        var authResponse = new AuthResponse(TestConstants.MockJwtToken, DateTime.UtcNow.AddHours(1), TestConstants.ValidUserId);
        var request = new LoginRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);
        authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Ok(authResponse));

        var result = await sut.Login(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(authResponse, ok.Value);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenCredentialsAreInvalid()
    {
        var (sut, authService) = BuildSut();
        var request = new LoginRequest(TestConstants.ValidEmail, "wrong");
        authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Fail(TestConstants.InvalidCredentialsMessage));

        var result = await sut.Login(request, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Me_ShouldReturn200WithProfile_WhenUserIsAuthenticated()
    {
        var (sut, authService) = BuildSut();
        var profile = new UserProfileResponse(TestConstants.ValidUserId, TestConstants.ValidEmail, DateTime.UtcNow);
        authService
            .Setup(s => s.GetCurrentUserAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserProfileResponse>.Ok(profile));

        var result = await sut.Me(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(profile, ok.Value);
    }

    [Fact]
    public async Task Me_ShouldReturn404_WhenUserNotFound()
    {
        var (sut, authService) = BuildSut();
        authService
            .Setup(s => s.GetCurrentUserAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserProfileResponse>.Fail(UserNotFoundError));

        var result = await sut.Me(CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
