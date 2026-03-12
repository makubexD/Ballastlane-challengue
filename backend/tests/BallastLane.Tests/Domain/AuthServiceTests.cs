using BallastLane.Application.DTOs;
using BallastLane.Application.Events;
using BallastLane.Application.Services;
using BallastLane.Application.Validators;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Events;
using BallastLane.Domain.Interfaces;
using BallastLane.Tests.TestData;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BallastLane.Tests.Domain;

public sealed class AuthServiceTests
{
    private static AuthService BuildSut(
        Mock<IUserRepository> userRepository,
        Mock<IPasswordHasher> passwordHasher,
        Mock<IJwtProvider> jwtProvider,
        Mock<IDateTimeProvider> clock,
        Mock<IDomainEventDispatcher> dispatcher) =>
        new(userRepository.Object, passwordHasher.Object, jwtProvider.Object,
            new AuthValidator(), clock.Object, NullLogger<AuthService>.Instance, dispatcher.Object);

    [Fact]
    public async Task Register_ShouldReturnCreatedUser_WhenEmailIsUniqueAndPasswordIsValid()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        userRepository
            .Setup(r => r.FindByEmailAsync(TestConstants.ValidEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        passwordHasher
            .Setup(h => h.Hash(TestConstants.ValidPassword))
            .Returns(TestConstants.HashedPassword);
        userRepository
            .Setup(r => r.SaveAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var result = await sut.RegisterAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestConstants.ValidEmail, result.Value.Email);
        passwordHasher.Verify(h => h.Hash(TestConstants.ValidPassword), Times.Once());
        userRepository.Verify(
            r => r.SaveAsync(
                It.Is<User>(u => u.PasswordHash == TestConstants.HashedPassword && u.Email == TestConstants.ValidEmail),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Register_ShouldHashPassword_NotStorePlainText()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        userRepository
            .Setup(r => r.FindByEmailAsync(TestConstants.ValidEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        passwordHasher
            .Setup(h => h.Hash(TestConstants.ValidPassword))
            .Returns(TestConstants.HashedPassword);
        userRepository
            .Setup(r => r.SaveAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var result = await sut.RegisterAsync(request);

        Assert.NotEqual(TestConstants.ValidPassword, result.Value.PasswordHash);
        passwordHasher.Verify(h => h.Hash(TestConstants.ValidPassword), Times.Once());
    }

    [Fact]
    public async Task Register_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var existingUser = User.Create(TestConstants.ValidUserId, TestConstants.ValidEmail, TestConstants.HashedPassword, TestConstants.FixedUtcNow);
        userRepository
            .Setup(r => r.FindByEmailAsync(TestConstants.ValidEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var result = await sut.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Conflict, result.ErrorType);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
        userRepository.Verify(
            r => r.SaveAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Register_ShouldReturnFailure_WhenPasswordIsTooShort()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new RegisterRequest(TestConstants.ValidEmail, "Ab1");

        var result = await sut.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("8 characters"));
    }

    [Fact]
    public async Task Register_ShouldReturnFailure_WhenPasswordLacksUppercase()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new RegisterRequest(TestConstants.ValidEmail, "lowercase1");

        var result = await sut.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("uppercase"));
    }

    [Fact]
    public async Task Register_ShouldReturnFailure_WhenPasswordLacksNumericCharacter()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new RegisterRequest(TestConstants.ValidEmail, "NoNumbers!");

        var result = await sut.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("numeric"));
    }

    [Fact]
    public async Task Login_ShouldReturnJwtToken_WhenCredentialsAreValid()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var user = User.Create(TestConstants.ValidUserId, TestConstants.ValidEmail, TestConstants.HashedPassword, TestConstants.FixedUtcNow);
        userRepository
            .Setup(r => r.FindByEmailAsync(TestConstants.ValidEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(h => h.Verify(TestConstants.ValidPassword, TestConstants.HashedPassword))
            .Returns(true);
        jwtProvider
            .Setup(j => j.Generate(It.IsAny<User>()))
            .Returns(TestConstants.MockJwtToken);
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new LoginRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var result = await sut.LoginAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestConstants.MockJwtToken, result.Value.Token);
        Assert.Equal(user.Id, result.Value.UserId);
        jwtProvider.Verify(j => j.Generate(It.Is<User>(u => u.Id == user.Id)), Times.Once());
    }

    [Fact]
    public async Task Login_ShouldReturnGenericFailure_WhenEmailNotRegistered()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        userRepository
            .Setup(r => r.FindByEmailAsync(TestConstants.ValidEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new LoginRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var result = await sut.LoginAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.ErrorType);
        Assert.Equal(TestConstants.InvalidCredentialsMessage, result.Errors[0]);
        Assert.DoesNotContain(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_ShouldReturnGenericFailure_WhenPasswordIsIncorrect()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var user = User.Create(TestConstants.ValidUserId, TestConstants.ValidEmail, TestConstants.HashedPassword, TestConstants.FixedUtcNow);
        userRepository
            .Setup(r => r.FindByEmailAsync(TestConstants.ValidEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(h => h.Verify(TestConstants.ValidPassword, TestConstants.HashedPassword))
            .Returns(false);
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new LoginRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        var result = await sut.LoginAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.ErrorType);
        Assert.Equal(TestConstants.InvalidCredentialsMessage, result.Errors[0]);
        jwtProvider.Verify(j => j.Generate(It.IsAny<User>()), Times.Never());
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUserProfile_WhenUserIdIsValid()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var user = User.Create(TestConstants.ValidUserId, TestConstants.ValidEmail, TestConstants.HashedPassword, TestConstants.FixedUtcNow);
        userRepository
            .Setup(r => r.FindByIdAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);

        var result = await sut.GetCurrentUserAsync(TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestConstants.ValidEmail, result.Value.Email);
        Assert.Equal(TestConstants.ValidUserId, result.Value.Id);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnFailure_WhenUserNotFound()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        userRepository
            .Setup(r => r.FindByIdAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);

        var result = await sut.GetCurrentUserAsync(TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Register_ShouldDispatchUserRegisteredEvent_WhenRegistrationSucceeds()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtProvider = new Mock<IJwtProvider>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        userRepository
            .Setup(r => r.FindByEmailAsync(TestConstants.ValidEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        passwordHasher
            .Setup(h => h.Hash(TestConstants.ValidPassword))
            .Returns(TestConstants.HashedPassword);
        userRepository
            .Setup(r => r.SaveAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var sut = BuildSut(userRepository, passwordHasher, jwtProvider, clock, dispatcher);
        var request = new RegisterRequest(TestConstants.ValidEmail, TestConstants.ValidPassword);

        await sut.RegisterAsync(request);

        dispatcher.Verify(
            d => d.DispatchAsync(
                It.Is<UserRegisteredEvent>(e => e.Email == TestConstants.ValidEmail),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
