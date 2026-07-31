using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Shared.CQRS;

namespace Modules.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedModule(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Singleton);

        foreach (var assembly in assemblies)
        {
            services.Scan(scan => scan.FromAssemblies(assembly)
                .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()

                .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()

                .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()

                .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime());
        }

        return services;
    }
}
