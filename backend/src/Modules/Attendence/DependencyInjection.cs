using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Attendence.Infrastructure.ZKTeco;
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

        services.AddScoped<IZKemSessionFactory, ZkemSessionFactory>();
        services.AddScoped<IAttendanceMachineReader>(sp =>
            new ZKTecoAttendanceMachineReader(
                sp.GetRequiredService<IZKemSessionFactory>())
        );

        services.AddSharedModule(typeof(DependencyInjection).Assembly);

        return services;
    }
}