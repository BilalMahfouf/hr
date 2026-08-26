using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.EnapPresistance;
using Modules.Employees.Infrastructure.Presistance;
using Modules.Shared.Infrastructure.Outbox;

namespace Modules.Employees;

public static class DependencyInjection
{
    public static IServiceCollection AddEmployeeModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<EmployeeDbContext>(
            (sp, options) =>
            {
                options
                    .UseNpgsql(
                        connectionString,
                        o => o.MigrationsHistoryTable(
                            HistoryRepository.DefaultTableName,
                            "employees"))
                    .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
            },
            ServiceLifetime.Scoped
        );

        services.AddScoped<IEmployeeDbContext>(sp =>
            sp.GetRequiredService<EmployeeDbContext>()
        );

        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IEmployeeRepository, EnapRepository>();
        services.AddScoped<IEmployeeGroupRepository, EmployeeGroupRepository>();
        services.AddScoped<Contracts.IEmployeeApi, Application.EmployeeApi>();

        return services;
    }
}
