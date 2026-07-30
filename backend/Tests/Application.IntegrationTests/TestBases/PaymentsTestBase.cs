using Application.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PublicApi.Features.Subscriptions.Webhooks;

namespace Application.IntegrationTests.TestBases;

public abstract class PaymentsTestBase : SubscriptionsTestBase
{
    protected PaymentsTestBase(PostgresFixture fixture) : base(fixture)
    {
    }

    protected HandleChargilyWebhook.CommandHandler CreateWebhookHandler(IServiceProvider services)
    {
        return new HandleChargilyWebhook.CommandHandler(
            services.GetRequiredService<PublicApi.Common.Abstracions.IApplicationDbContext>(),
            NullLogger<HandleChargilyWebhook.CommandHandler>.Instance);
    }
}
