using Microsoft.Extensions.DependencyInjection;

namespace Modules.Employees;

public static class DependencyInjection
{
    public static IServiceCollection AddEmployeeModule(this IServiceCollection services)
    {
        services.AddScoped<Contracts.IEmployeeApi, EmployeeApi>();

        return services;
    }
}
