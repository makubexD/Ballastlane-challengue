using BallastLane.Application.Services;
using BallastLane.Application.Validators;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Auth;
using BallastLane.Infrastructure.Common;
using BallastLane.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();

        services.AddScoped<ITaskRepository, SqlTaskRepository>();
        services.AddScoped<IUserRepository, SqlUserRepository>();

        services.AddSingleton<TaskValidator>();
        services.AddSingleton<AuthValidator>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("Database")
                ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");
            return new DatabaseMigrator(connStr);
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("Database")
                ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");
            var hasher = sp.GetRequiredService<IPasswordHasher>();
            return new DatabaseSeeder(connStr, hasher);
        });

        return services;
    }
}
