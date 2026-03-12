using BallastLane.Domain.Common;

namespace BallastLane.Domain.ValueObjects;

public sealed class TaskTitle : IEquatable<TaskTitle>
{
    public const int MaxLength = 200;
    public string Value { get; }

    private TaskTitle(string value) => Value = value;

    public static Result<TaskTitle> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<TaskTitle>.Fail("Title must not be empty.", ResultErrorType.Validation);

        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength)
            return Result<TaskTitle>.Fail($"Title must not exceed {MaxLength} characters.", ResultErrorType.Validation);

        return Result<TaskTitle>.Ok(new TaskTitle(trimmed));
    }

    public bool Equals(TaskTitle? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is TaskTitle other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

    public static implicit operator string(TaskTitle title) => title.Value;
}
