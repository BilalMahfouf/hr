using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Resend;
using Modules.Shared.Abstracions;
using Modules.Shared.Abstracions.Emails;
using Modules.Shared.CQRS;
using Modules.Shared.Infrastructure.CQRS;
using Modules.Shared.Infrastructure.Emails;
using Modules.Shared.Infrastructure.Exceptions;
using Modules.Shared.Infrastructure.Interceptors;
using Modules.Shared.Infrastructure.Outbox;
using Modules.Shared.Infrastructure.Persistence;
using Modules.Shared.Infrastructure.Services;

namespace Modules.Shared.Infrastructure;

/// <summary>
/// Provides the <see cref="AddSharedInfrastructure"/> extension method that wires up all
/// shared infrastructure dependencies used by every module:
/// <list type="bullet">
///   <item>EF Core interceptors (<c>AuditInterceptor</c>, <c>InsertOutboxMessagesInterceptors</c>)</item>
///   <item><see cref="SharedDbContext"/> — owner of the <c>shared</c> schema (outbox) migrations</item>
///   <item>Current-user service (<see cref="ICurrentUser"/>)</item>
///   <item>Domain event dispatcher/publisher for the outbox processor</item>
///   <item>RFC 7807 exception handlers (validation → domain → global, order matters)</item>
///   <item>Email services (Resend primary; MailKit SMTP fallback available)</item>
///   <item>Quartz <see cref="ProcessOutboxMessagesJob"/> (every 10 seconds)</item>
/// </list>
/// The host must still register <c>AddProblemDetails()</c>, <c>UseExceptionHandler()</c>
/// and the Quartz hosted service (<c>AddQuartzHostedService</c>) once.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // EF Core interceptors (resolved by every module DbContext registration)
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<InsertOutboxMessagesInterceptors>();

        // Shared DbContext — owns the shared schema (outbox_messages) migrations
        services.AddDbContext<SharedDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(
                    connectionString,
                    o => o.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        "shared"))
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
        });

        // Factory used by the outbox processor job (outside of any request scope)
        services.AddDbContextFactory<SharedDbContext>(
            (sp, options) =>
            {
                options
                    .UseNpgsql(
                        connectionString,
                        o => o.MigrationsHistoryTable(
                            HistoryRepository.DefaultTableName,
                            "shared"))
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
            },
            ServiceLifetime.Scoped
        );

        // Current user
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();

        // Domain event plumbing
        services.AddTransient<IDomainEventDispatcher, DomainEventsDispatcher>();
        services.AddTransient<IDomainEventPublisher, DomainEventPublisher>();

        // Exception handlers (registration order = execution order)
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Email options (SMTP fallback configuration)
        services.Configure<EmailOptions>(options =>
        {
            var portString =
                Environment.GetEnvironmentVariable("EMAIL_CONFIGURATIONS_PORT")
                ?? throw new InvalidOperationException(
                    "EMAIL_CONFIGURATIONS_PORT environment variable is not set"
                );

            int.TryParse(portString, out int port);
            options.Port = port;
            options.Host =
                Environment.GetEnvironmentVariable("EMAIL_CONFIGURATIONS_HOST")
                ?? throw new InvalidOperationException("EMAIL_CONFIGURATIONS_HOST is not set");
            options.Password =
                Environment.GetEnvironmentVariable("EMAIL_CONFIGURATIONS_PASSWORD")
                ?? throw new InvalidOperationException(
                    "EMAIL_CONFIGURATIONS_PASSWORD environment variable is not set"
                );
            options.Email =
                Environment.GetEnvironmentVariable("EMAIL_CONFIGURATIONS_EMAIL")
                ?? throw new InvalidOperationException(
                    "EMAIL_CONFIGURATIONS_EMAIL environment variable is not set"
                );
        });

        // Resend (primary email provider)
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN")!;
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddTransient<IEmailService, ResendEmailService>();

        // Quartz: outbox processor — runs every 10 seconds.
        // The host registers AddQuartzHostedService once; this AddQuartz call composes
        // with any module/host-level Quartz configuration.
        services.AddQuartz(configure =>
        {
            var outboxJobKey = new JobKey(nameof(ProcessOutboxMessagesJob));
            configure
                .AddJob<ProcessOutboxMessagesJob>(
                    (sp, opts) =>
                    {
                        opts.WithIdentity(outboxJobKey);
                    }
                )
                .AddTrigger(trigger =>
                    trigger
                        .ForJob(outboxJobKey)
                        .WithSimpleSchedule(schedule =>
                            schedule.WithIntervalInSeconds(10).RepeatForever()
                        )
                );
        });

        return services;
    }
}
