namespace BallastLane.Domain.Common;

public enum ResultErrorType
{
    None = 0,
    Validation,
    NotFound,
    Conflict,
    Unauthorized
}
