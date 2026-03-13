using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Infrastructure.Common;

/// <summary>
/// Encapsulates all DI registrations for a specific database provider.
/// Implement this interface to add a new provider with zero changes to existing code.
/// </summary>
public interface IDbProviderRegistrar
{
    /// <summary>Matches the value of ConnectionStrings:Provider in configuration.</summary>
    string ProviderName { get; }

    /// <summary>Registers the connection factory, unit of work, and repositories for this provider.</summary>
    void Register(IServiceCollection services);
}
