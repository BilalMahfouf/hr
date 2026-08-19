using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Modules.Identity.Abstracions;
using Modules.Identity.Infrastructure.Persistence;
using Modules.Shared.Abstracions;
using Modules.Shared.Infrastructure.Outbox;
using PublicApi.Common.Abstracions;
using PublicApi.Domain.Notifications;
using PublicApi.Features.Subscriptions.BackgroundJobs;
using PublicApi.Infrastructure.Notifications;
using PublicApi.Infrastructure.Persistence;
using PublicApi.Infrastructure.Services.Hashers;
using PublicApi.Infrastructure.Services.Subscriptions;

namespace PublicApi.Infrastructure;

/// <summary>
/// Provides the <see cref="AddInfrastructure"/> extension method that wires up the
/// application-specific infrastructure dependencies for the Veterinary API.
/// Shared infrastructure (outbox, shared DbContext, CQRS event plumbing, exception
/// handlers, email, current user) lives in <c>Modules.Shared</c> and is registered
/// via <c>AddSharedInfrastructure</c>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the application infrastructure services including:
    /// <list type="bullet">
    ///   <item>Password hashing (<see cref="Argon2PasswordHasher"/>)</item>
    ///   <item>JWT authentication (HMAC-SHA256, read from environment variables)</item>
    ///   <item>EF Core + Npgsql for <see cref="ApplicationDbContext"/> and <c>IdentityDbContext</c>,
    ///   both with <c>InsertOutboxMessagesInterceptors</c> (registered by the shared module)</item>
    ///   <item>Subscription status query (<c>UserSubscriptionStatusQuery</c>)</item>
    ///   <item>Quartz.NET subscription background jobs (daily)</item>
    ///   <item>SignalR + <see cref="INotificatioService"/> (<c>NotificationService</c>)</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

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

        // ef core config

        var connectionString = Environment.GetEnvironmentVariable("DefaultConnectionLocal");
        services.AddDbContext<IApplicationDbContext, ApplicationDbContext>(
            (sp, options) =>
            {
                options
                    .UseNpgsql(connectionString)
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
            },
            ServiceLifetime.Scoped
        );

        // Identity EF Core DbContext (separate schema, gets same interceptors)
        services.AddDbContext<IdentityDbContext>(
            (sp, options) =>
            {
                options
                    .UseNpgsql(
                        connectionString,
                        o => o.MigrationsHistoryTable(
                            HistoryRepository.DefaultTableName,
                            "identity"))
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
            },
            ServiceLifetime.Scoped
        );

        services.AddScoped<IIdentityApplicationDbContext>(sp =>
            sp.GetRequiredService<IdentityDbContext>()
        );

        services.AddScoped<IUserSubscriptionStatusQuery, UserSubscriptionStatusQuery>();

        // Quartz Background jobs (subscription lifecycle — daily at 00:00 AM UTC).
        // The outbox processor job is registered by the shared module.
        services.AddQuartz(configure =>
        {
            // Mark past due subscriptions
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

            // Mark expired subscriptions
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

            // Clean pending subscriptions
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

        return services;
    }
}
