using BallastLane.Application.DTOs;
using FluentValidation;

namespace BallastLane.Application.Validators;

/// <summary>
/// Validates <see cref="RegisterRequest"/> using FluentValidation rules.
/// Password must be at least 8 characters, contain an uppercase letter, and a numeric character.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("A valid email address is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password must be at least 8 characters.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least one numeric character.");
    }

    public new IReadOnlyList<string> Validate(RegisterRequest request)
    {
        var result = base.Validate(request);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }
}

/// <summary>
/// Validates <see cref="LoginRequest"/> ensuring email and password are present.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }

    public new IReadOnlyList<string> Validate(LoginRequest request)
    {
        var result = base.Validate(request);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }
}

/// <summary>
/// Wraps <see cref="RegisterRequestValidator"/> and <see cref="LoginRequestValidator"/>
/// to preserve the service-layer contract used by <see cref="BallastLane.Application.Services.AuthService"/>.
/// </summary>
public sealed class AuthValidator
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    public IReadOnlyList<string> Validate(RegisterRequest request) =>
        _registerValidator.Validate(request);

    public IReadOnlyList<string> Validate(LoginRequest request) =>
        _loginValidator.Validate(request);
}
