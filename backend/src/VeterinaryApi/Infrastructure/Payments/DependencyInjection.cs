using Chargily.Pay;
using Chargily.Pay.AspNet;
using VeterinaryApi.Common.Abstracions.Payments;

namespace VeterinaryApi.Infrastructure.Payments;

public static class DependencyInjection
{
    public static IServiceCollection AddPayments(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, ChargilyPaymentService>();

        var chargilySecretKey = Environment.GetEnvironmentVariable("CHARGILY_SECRET_KEY");
        services.AddGlobalChargilyPayClient(config =>
        {
            config.IsLiveMode = true;
            config.ApiSecretKey = chargilySecretKey ??
            throw new InvalidOperationException(
                "CHARGILY_SECRET_KEY environment variable is not set.");
        })
         .AddChargilyPayWebhookValidationMiddleware();

        services.Configure<ChargilyOptions>(options =>
{
    options.ApiSecretKey = Environment.GetEnvironmentVariable("CHARGILY_SECRET_KEY")
        ?? throw new InvalidOperationException(
            "CHARGILY_API_SECRET_KEY environment variable is not set.");

    options.IsLiveMode = bool.Parse(
        Environment.GetEnvironmentVariable("CHARGILY_IS_LIVE_MODE") ?? "false");

    options.WebhookUrl = Environment.GetEnvironmentVariable("CHARGILY_WEBHOOK_URL")
        ?? throw new InvalidOperationException(
            "CHARGILY_WEBHOOK_URL environment variable is not set.");

    options.SuccessUrl = Environment.GetEnvironmentVariable("CHARGILY_SUCCESS_URL")
        ?? throw new InvalidOperationException(
            "CHARGILY_SUCCESS_URL environment variable is not set.");

    options.FailureUrl = Environment.GetEnvironmentVariable("CHARGILY_FAILURE_URL")
        ?? throw new InvalidOperationException(
            "CHARGILY_FAILURE_URL environment variable is not set.");
});


        return services;
    }
    public static WebApplication UsePayments(this WebApplication app)
    {
        app.UseChargilyPayWebhookValidation();
        return app;
    }
}
