namespace BallastLane.Domain.Common;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, IReadOnlyList<string> errors, ResultErrorType errorType)
    {
        IsSuccess = isSuccess;
        _value = value;
        Errors = errors;
        ErrorType = errorType;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }
    public ResultErrorType ErrorType { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public static Result<T> Ok(T value) => new(true, value, [], ResultErrorType.None);
    public static Result<T> Fail(string error, ResultErrorType errorType = ResultErrorType.Validation) => new(false, default, [error], errorType);
    public static Result<T> Fail(IReadOnlyList<string> errors) => new(false, default, errors, ResultErrorType.Validation);
}
