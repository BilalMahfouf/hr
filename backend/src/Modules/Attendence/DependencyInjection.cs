using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Application.PunchPolling;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.PunchPolling;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Attendence.Infrastructure.Quartz;
using Modules.Attendence.Infrastructure.ZKTeco;
using Modules.Attendence.Infrastructure.ZKTeco.Gateway;
using Modules.Shared;
using Modules.Shared.Infrastructure.Outbox;

namespace Modules.Attendence;

public static class DependencyInjection
{
    public static IServiceCollection AddAttendenceModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AttendanceDbContext>(
            (sp, options) =>
            {
                options
                    .UseNpgsql(
                        connectionString,
                        o => o.MigrationsHistoryTable(
                            HistoryRepository.DefaultTableName,
                            "attendance"))
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
            },
            ServiceLifetime.Scoped
        );

        services.AddScoped<IAttendanceDbContext>(sp =>
            sp.GetRequiredService<AttendanceDbContext>()
        );
        var zktecoBaseUrl = Environment.GetEnvironmentVariable("ZKTECO_GATEWAY_BASE_URL")?.TrimEnd('/')
                ?? throw new InvalidOperationException(
                "ZKTECO_GATEWAY_BASE_URL environment variable is not set.");

        services.Configure<ZKTecoGatewayOptions>(options =>
        {
            options.BaseUrl = zktecoBaseUrl;
        });
        Console.WriteLine($"ZKTeco Gateway BaseUrl: {zktecoBaseUrl}");

        services.AddHttpClient<ZKTecoGatwayMachineReader>((sp, client) =>
        {
            client.BaseAddress = new Uri($"{zktecoBaseUrl}/");
        });

        services.AddScoped<IZKemSessionFactory, ZkemSessionFactory>();
        services.AddScoped<ZKTecoAttendanceMachineReader>();
        services.AddScoped<IAttendanceMachineReaderFactory, AttendanceMachineReaderFactory>();

        // Punch polling scheduler (Quartz)
        services.AddSingleton<IPunchPollingScheduler, QuartzPunchPollingScheduler>();
        services.AddHostedService<PunchPollingStartupService>();

        services.AddSharedModule(typeof(DependencyInjection).Assembly);

        return services;
    }
}