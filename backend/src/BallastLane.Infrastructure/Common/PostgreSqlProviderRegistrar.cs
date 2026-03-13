using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Infrastructure.Common;

public sealed class PostgreSqlProviderRegistrar : IDbProviderRegistrar
{
    public string ProviderName => "PostgreSQL";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<NpgsqlUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NpgsqlUnitOfWork>());
        services.AddScoped<ITaskRepository>(sp => sp.GetRequiredService<NpgsqlUnitOfWork>().TaskRepository);
        services.AddScoped<IUserRepository, SqlUserRepository>();
    }
}
