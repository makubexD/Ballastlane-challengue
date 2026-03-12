using System.Text.RegularExpressions;
using BallastLane.Application.DTOs;

namespace BallastLane.Application.Validators;

public sealed class AuthValidator
{
    private const int MinPasswordLength = 8;
    private static readonly Regex UppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex NumericRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<string> Validate(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex.IsMatch(request.Email))
            errors.Add("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinPasswordLength)
            errors.Add($"Password must be at least {MinPasswordLength} characters.");

        if (!string.IsNullOrEmpty(request.Password) && !UppercaseRegex.IsMatch(request.Password))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!string.IsNullOrEmpty(request.Password) && !NumericRegex.IsMatch(request.Password))
            errors.Add("Password must contain at least one numeric character.");

        return errors;
    }

    public IReadOnlyList<string> Validate(LoginRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required.");

        return errors;
    }
}
