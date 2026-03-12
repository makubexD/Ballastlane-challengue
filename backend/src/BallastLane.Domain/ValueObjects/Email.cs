using BallastLane.Domain.Common;

namespace BallastLane.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Email>.Fail("Email must not be empty.", ResultErrorType.Validation);

        var trimmed = raw.Trim();

        var atIndex = trimmed.IndexOf('@');
        if (atIndex <= 0 || atIndex == trimmed.Length - 1)
            return Result<Email>.Fail("Email format is invalid.", ResultErrorType.Validation);

        return Result<Email>.Ok(new Email(trimmed.ToLowerInvariant()));
    }

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
