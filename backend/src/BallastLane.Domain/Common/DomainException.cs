namespace BallastLane.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, object id)
        : base($"{resourceName} with id '{id}' was not found.") { }
}

public sealed class ValidationException : DomainException
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public ValidationException(IReadOnlyList<string> errors)
        : base($"Validation failed: {string.Join("; ", errors)}")
    {
        ValidationErrors = errors;
    }
}
