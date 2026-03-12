namespace BallastLane.Domain.Common;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        _value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public static Result<T> Ok(T value) => new(true, value, []);
    public static Result<T> Fail(string error) => new(false, default, [error]);
    public static Result<T> Fail(IReadOnlyList<string> errors) => new(false, default, errors);
}
