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

        // NpgsqlUnitOfWork is scoped — one per HTTP request.
        // UnitOfWorkMiddleware calls BeginAsync before each request reaches a controller.
        services.AddScoped<NpgsqlUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NpgsqlUnitOfWork>());

        // AuthService uses IUserRepository directly (no write transaction needed there).
        services.AddScoped<IUserRepository, SqlUserRepository>();

        services.AddSingleton<TaskValidator>();
        services.AddSingleton<AuthValidator>();
        services.AddScoped<TaskService>();
        services.AddScoped<ITaskCommandService>(sp => sp.GetRequiredService<TaskService>());
        services.AddScoped<ITaskQueryService>(sp => sp.GetRequiredService<TaskService>());
        services.AddScoped<IAuthService, AuthService>();

        services.AddSingleton<DatabaseMigrator>();
        services.AddSingleton<DatabaseSeeder>();

        return services;
    }
}
