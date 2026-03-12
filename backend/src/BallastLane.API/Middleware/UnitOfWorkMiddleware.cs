using BallastLane.Infrastructure.Persistence;

namespace BallastLane.API.Middleware;

/// <summary>
/// Opens the unit of work transaction at the beginning of each request
/// so that all services share a single connection and transaction.
/// </summary>
public sealed class UnitOfWorkMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, NpgsqlUnitOfWork unitOfWork)
    {
        await unitOfWork.BeginAsync(context.RequestAborted);
        await next(context);
    }
}
