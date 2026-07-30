using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using Modules.Identity.Infrastructure.Auth;

namespace Modules.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.Configure<JwtOptions>(options =>
        {
            options.SingingKey =
                Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException(
                    "JWT_SECRET_KEY environment variable is not set"
                );
            options.Issuer =
                Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? throw new InvalidOperationException(
                    "JWT_ISSUER environment variable is not set"
                );
            options.Audience =
                Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? throw new InvalidOperationException(
                    "JWT_AUDIENCE environment variable is not set"
                );
            options.LifeTime = byte.Parse(
                Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_LIFETIME_MINUTES") ?? "15"
            );
        });
        services.AddScoped<IJwtProvider, JwtProvider>();

        return services;
    }
}
