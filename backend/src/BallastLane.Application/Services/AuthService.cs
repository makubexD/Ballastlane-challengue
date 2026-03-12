using BallastLane.Application.DTOs;
using BallastLane.Application.Validators;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    AuthValidator validator,
    IDateTimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<Result<User>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return Result<User>.Fail(errors);

        var existing = await userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result<User>.Fail("An account with this email already exists.");

        var hash = passwordHasher.Hash(request.Password);
        var user = User.Create(Guid.NewGuid(), request.Email, hash, clock.UtcNow);
        var saveResult = await userRepository.SaveAsync(user, cancellationToken);

        if (!saveResult.IsSuccess)
            return Result<User>.Fail(saveResult.Errors);

        logger.LogInformation("User registered: {Email}", request.Email);
        return Result<User>.Ok(user);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return Result<AuthResponse>.Fail(errors);

        var user = await userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Failed login attempt for: {Email}", request.Email);
            return Result<AuthResponse>.Fail(InvalidCredentialsMessage);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for: {Email}", request.Email);
            return Result<AuthResponse>.Fail(InvalidCredentialsMessage);
        }

        var token = jwtProvider.Generate(user);
        var expiresAt = clock.UtcNow.AddMinutes(jwtProvider.ExpiryMinutes);
        logger.LogInformation("User logged in: {UserId}", user.Id);
        return Result<AuthResponse>.Ok(new AuthResponse(token, expiresAt, user.Id));
    }

    public async Task<Result<UserProfileResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<UserProfileResponse>.Fail($"User '{userId}' was not found.");

        return Result<UserProfileResponse>.Ok(new UserProfileResponse(user.Id, user.Email, user.CreatedAt));
    }
}
