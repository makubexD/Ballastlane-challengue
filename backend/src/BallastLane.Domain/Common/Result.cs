namespace BallastLane.Domain.Common;

public sealed class Result
{
    private static readonly Result SuccessInstance = new(true, [], ResultErrorType.None);

    private Result(bool isSuccess, IReadOnlyList<string> errors, ResultErrorType errorType)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        ErrorType = errorType;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }
    public ResultErrorType ErrorType { get; }

    public static Result Ok() => SuccessInstance;
    public static Result Fail(string error, ResultErrorType errorType = ResultErrorType.Validation) => new(false, [error], errorType);
    public static Result Fail(IReadOnlyList<string> errors) => new(false, errors, ResultErrorType.Validation);
}
