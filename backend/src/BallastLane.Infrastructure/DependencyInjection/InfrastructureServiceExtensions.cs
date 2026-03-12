using BallastLane.Application.CQRS;
using BallastLane.Application.EventHandlers;
using BallastLane.Application.Events;
using BallastLane.Application.Services;
using BallastLane.Application.Tasks.Commands;
using BallastLane.Application.Tasks.Handlers;
using BallastLane.Application.Tasks.Queries;
using BallastLane.Application.Validators;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Events;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Auth;
using BallastLane.Infrastructure.Common;
using BallastLane.Infrastructure.CQRS;
using BallastLane.Infrastructure.Events;
using BallastLane.Infrastructure.Persistence;
using BallastLane.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedSettings>()
            .Bind(configuration.GetSection(SeedSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();

        // NpgsqlUnitOfWork is scoped — one per HTTP request.
        // UnitOfWorkMiddleware calls BeginAsync before each request reaches a controller.
        services.AddScoped<NpgsqlUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NpgsqlUnitOfWork>());

        // ITaskRepository resolves to the transactional repository held inside NpgsqlUnitOfWork.
        // BeginAsync (called by UnitOfWorkMiddleware) must run before TaskService is invoked.
        services.AddScoped<ITaskRepository>(sp => sp.GetRequiredService<NpgsqlUnitOfWork>().TaskRepository);

        // AuthService uses IUserRepository directly (no write transaction needed there).
        services.AddScoped<IUserRepository, SqlUserRepository>();

        services.AddSingleton<TaskValidator>();
        services.AddSingleton<AuthValidator>();
        services.AddScoped<TaskService>();
        services.AddScoped<ITaskCommandService>(sp => sp.GetRequiredService<TaskService>());
        services.AddScoped<ITaskQueryService>(sp => sp.GetRequiredService<TaskService>());
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<TaskCreatedEvent>, TaskCreatedEventHandler>();
        services.AddScoped<IDomainEventHandler<TaskDeletedEvent>, TaskDeletedEventHandler>();
        services.AddScoped<IDomainEventHandler<UserRegisteredEvent>, UserRegisteredEventHandler>();

        services.AddSingleton<DatabaseMigrator>();
        services.AddSingleton<DatabaseSeeder>();

        // CQRS dispatchers
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Task command handlers
        services.AddScoped<ICommandHandler<CreateTaskCommand, TaskItem>, CreateTaskCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateTaskCommand, TaskItem>, UpdateTaskCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteTaskCommand>, DeleteTaskCommandHandler>();

        // Task query handlers
        services.AddScoped<IQueryHandler<GetTaskByIdQuery, TaskItem>, GetTaskByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetAllTasksQuery, IReadOnlyList<TaskItem>>, GetAllTasksQueryHandler>();

        return services;
    }
}
