using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Infrastructure.Common;

public sealed class SqliteProviderRegistrar : IDbProviderRegistrar
{
    public string ProviderName => "SQLite";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
        services.AddScoped<SqliteUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SqliteUnitOfWork>());
        services.AddScoped<ITaskRepository>(sp => sp.GetRequiredService<SqliteUnitOfWork>().TaskRepository);
        services.AddScoped<IUserRepository, SqliteUserRepository>();
    }
}
