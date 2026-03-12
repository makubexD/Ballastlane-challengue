using BallastLane.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace BallastLane.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);

        return result.Errors.Count == 1 && result.Errors[0].Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? new NotFoundObjectResult(new { errors = result.Errors })
            : new BadRequestObjectResult(new { errors = result.Errors });
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return result.Errors.Count == 1 && result.Errors[0].Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? new NotFoundObjectResult(new { errors = result.Errors })
            : new BadRequestObjectResult(new { errors = result.Errors });
    }
}
