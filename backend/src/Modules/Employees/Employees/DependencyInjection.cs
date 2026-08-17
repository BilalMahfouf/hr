using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.EnapPresistance;

namespace Modules.Employees;

public static class DependencyInjection
{
    public static IServiceCollection AddEmployeeModule(this IServiceCollection services)
    {
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IEmployeeRepository, EnapRepository>();
        services.AddScoped<Contracts.IEmployeeApi, Application.EmployeeApi>();

        return services;
    }
}
