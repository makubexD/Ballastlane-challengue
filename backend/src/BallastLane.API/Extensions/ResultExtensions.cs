using BallastLane.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace BallastLane.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound     => new NotFoundObjectResult(ToProblemDetails(404, "Not Found", result.Errors)),
            ResultErrorType.Conflict     => new ConflictObjectResult(ToProblemDetails(409, "Conflict", result.Errors)),
            ResultErrorType.Unauthorized => new UnauthorizedObjectResult(ToProblemDetails(401, "Unauthorized", result.Errors)),
            _                            => new BadRequestObjectResult(ToProblemDetails(400, "Bad Request", result.Errors))
        };
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return result.ErrorType switch
        {
            ResultErrorType.NotFound     => new NotFoundObjectResult(ToProblemDetails(404, "Not Found", result.Errors)),
            ResultErrorType.Conflict     => new ConflictObjectResult(ToProblemDetails(409, "Conflict", result.Errors)),
            ResultErrorType.Unauthorized => new UnauthorizedObjectResult(ToProblemDetails(401, "Unauthorized", result.Errors)),
            _                            => new BadRequestObjectResult(ToProblemDetails(400, "Bad Request", result.Errors))
        };
    }

    private static ProblemDetails ToProblemDetails(int status, string title, IReadOnlyList<string> errors) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = errors.Count == 1 ? errors[0] : string.Join("; ", errors)
        };
}
