using System.Diagnostics;
using System.Text;
using Modules.Identity.Abstracions;
using Modules.Identity.Application.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Resend;
using Modules.Shared;
using Modules.Shared.Abstracions;
using Modules.Shared.Abstracions.Emails;
using Modules.Shared.CQRS;
using PublicApi.Common.Abstracions;
using PublicApi.Domain.Notifications;
using PublicApi.Features.Subscriptions.BackgroundJobs;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.Identity.Infrastructure.Persistence;
using PublicApi.Infrastructure.CQRS;
using PublicApi.Infrastructure.Interceptors;
using PublicApi.Infrastructure.Notifications;
using PublicApi.Infrastructure.OutboxMessages;
using PublicApi.Infrastructure.Persistence;
using PublicApi.Infrastructure.Services.Hashers;
using PublicApi.Infrastructure.Services.Notifications;
using PublicApi.Infrastructure.Services.Subscriptions;
using PublicApi.Infrastructure.Services.Users;
using PublicApi.Infrastructure.Tenants;

namespace PublicApi.Infrastructure;

/// <summary>
/// Provides the <see cref="AddInfrastructure"/> extension method that wires up all
/// infrastructure dependencies for the Veterinary API.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services including:
    /// <list type="bullet">
    ///   <item>Password hashing (<see cref="Argon2PasswordHasher"/>)</item>
    ///   <item>JWT authentication (HMAC-SHA256, read from environment variables)</item>
    ///   <item>EF Core + Npgsql with <c>AuditInterceptor</c>, <c>InsertOutboxMessagesInterceptors</c>, and <c>TenantInterceptor</c></item>
    ///   <item>Email service (<c>EmailService</c>)</item>
    ///   <item>Tenant/current-user service (<c>CurrentUserService</c>)</item>
    ///   <item>CQRS dispatchers and handlers (auto-scanned)</item>
    ///   <item>Quartz.NET background job (outbox processor, every 10 seconds)</item>
    ///   <item>SignalR + <see cref="INotificatioService"/> (<c>NotificationService</c>)</item>
    ///   <item>Domain event handlers (scanned from the entry assembly)</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

        var ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        var ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");

        // auth config
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                    ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            Environment.GetEnvironmentVariable("JWT_SECRET_KEY")!
                        )
                    ),
                    ClockSkew = TimeSpan.Zero,
                };

                //SignalR sends token via query string for WebSocket connections

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        // interceptors config

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<InsertOutboxMessagesInterceptors>();
        services.AddScoped<TenantInterceptor>();

        // ef core config

        var connectionString = Environment.GetEnvironmentVariable("DefaultConnectionLocal");
        services.AddDbContext<IApplicationDbContext, ApplicationDbContext>(
            (sp, options) =>
            {
                options
                    .UseNpgsql(connectionString)
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>())
                    .AddInterceptors(sp.GetRequiredService<TenantInterceptor>());
            },
            ServiceLifetime.Scoped
        );

        // Identity EF Core DbContext (separate schema, gets same TenantInterceptor)
        services.AddDbContext<IdentityDbContext>(
            (sp, options) =>
            {
                options.UseNpgsql(connectionString, o =>
                    o.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        "identity"))
                    .AddInterceptors(sp.GetRequiredService<TenantInterceptor>());
            },
            ServiceLifetime.Scoped
        );

        services.AddScoped<IIdentityApplicationDbContext>(sp =>
            sp.GetRequiredService<IdentityDbContext>()
        );

        services.AddScoped<IUserSubscriptionStatusQuery, UserSubscriptionStatusQuery>();

        // Email Options config
        services.Configure<EmailOptions>(options =>
        {
            Console.WriteLine("Configuring EmailOptions from environment variables...");
            var portString =
                Environment.GetEnvironmentVariable("EMAIL_CONFIGURATIONS_PORT")
                ?? throw new InvalidOperationException();

            int.TryParse(portString, out int port);
            options.Port = port;
            Console.WriteLine($"EMAIL_CONFIGURATIONS_PORT: {options.Port}");
            options.Host =
                Environment.GetEnvironmentVariable("EMAIL_CONFIGURATIONS_HOST")
                ?? throw new InvalidOperationException("EMAIL_CONFIGURATIONS_HOST is not set");
            Console.WriteLine($"EMAIL_CONFIGURATIONS_HOST: {options.Host}");
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

        services.AddScoped<ICurrentTenant, CurrentUserService>();

        services.AddHttpContextAccessor();

        // CQRS
        services.AddTransient<IDomainEventDispatcher, DomainEventsDispatcher>();

        // Quartz Background job
        services.AddQuartz(configure =>
        {
            // Outbox processor — runs every 10 seconds
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

            // Mark past due subscriptions — runs every day at 00:00 AM UTC
            var markPastDueSubscriptionJobKey = new JobKey(nameof(MarkPastDueSubscriptionDailyJob));
            configure
                .AddJob<MarkPastDueSubscriptionDailyJob>(
                    (sp, opts) =>
                    {
                        opts.WithIdentity(markPastDueSubscriptionJobKey);
                    }
                )
                .AddTrigger(trigger =>
                {
                    trigger.ForJob(markPastDueSubscriptionJobKey).WithCronSchedule("0 0 0 * * ?");
                });

            // Mark expired subscriptions — runs every day at 00:00 AM UTC
            var markExpiredSubscriptionJobKey = new JobKey(nameof(MarkExpiredSubscriptionDailyJob));
            configure
                .AddJob<MarkExpiredSubscriptionDailyJob>(
                    (sp, opts) =>
                    {
                        opts.WithIdentity(markExpiredSubscriptionJobKey);
                    }
                )
                .AddTrigger(trigger =>
                {
                    trigger.ForJob(markExpiredSubscriptionJobKey).WithCronSchedule("0 0 0 * * ?");
                });

            var cleanPendingSubscriptionsJobKey = new JobKey(
                nameof(CleanPendingSubscriptionsDailyJob)
            );
            configure
                .AddJob<CleanPendingSubscriptionsDailyJob>(
                    (sp, opts) =>
                    {
                        opts.WithIdentity(cleanPendingSubscriptionsJobKey);
                    }
                )
                .AddTrigger(trigger =>
                {
                    trigger.ForJob(cleanPendingSubscriptionsJobKey).WithCronSchedule("0 0 0 * * ?");
                });
        });
        services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

        services.AddSignalR();
        services.AddScoped<INotificatioService, NotificationService>();
        var identityEventHandlerAssembly = typeof(Register).Assembly;
        services.Scan(scan =>
            scan.FromAssembliesOf(typeof(Program))
                .AddClasses(
                    classes => classes.AssignableTo(typeof(IDomainEventHandler<>)),
                    publicOnly: false
                )
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );
        services.Scan(scan =>
            scan.FromAssemblies(identityEventHandlerAssembly)
                .AddClasses(
                    classes => classes.AssignableTo(typeof(IDomainEventHandler<>)),
                    publicOnly: false
                )
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );
        services.AddTransient<IDomainEventPublisher, DomainEventPublisher>();

        // resend for sending emails :
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN")!;
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddTransient<IEmailService, ResendEmailService>();

        return services;
    }
}
