namespace BallastLane.Domain.Common;

public sealed class Result
{
    private static readonly Result SuccessInstance = new(true, []);

    private Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    public static Result Ok() => SuccessInstance;
    public static Result Fail(string error) => new(false, [error]);
    public static Result Fail(IReadOnlyList<string> errors) => new(false, errors);
}
