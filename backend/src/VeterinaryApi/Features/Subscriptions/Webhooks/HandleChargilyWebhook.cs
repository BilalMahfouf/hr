using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using Shared.CQRS;
using Shared.Endpoints;
using Shared.Errors;
using Shared.Results;
using VeterinaryApi.Domain.Subscriptions;

namespace VeterinaryApi.Features.Subscriptions.Webhooks;

public static class HandleChargilyWebhook
{
    public sealed record Command(string RawBody) : ICommand;
    public sealed class ChargilyWebhookPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("data")]
        public ChargilyCheckoutData Data { get; set; } = null!;
    }

    public sealed class ChargilyCheckoutData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("metadata")]
        public List<string>? Metadata { get; set; }

        [JsonPropertyName("failure_reason")]
        public string? FailureReason { get; set; }
    }

    public sealed class CommandHandler(
        IApplicationDbContext db,
        ILogger<CommandHandler> logger)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.RawBody))
            {
                return Result.Failure(Error
                    .Failure("Chargily.BodyNull", "the request body is null"));
            }
            var payload = JsonSerializer
                .Deserialize<ChargilyWebhookPayload>(command.RawBody);

            if (payload is null)
            {
                return Result.Failure(Error
                    .Failure("Chargily.PayloadNull", "there is no paylod"));
            }

            switch (payload.Type)
            {
                case "checkout.paid":
                    logger.LogInformation("checkout is paid");
                    await HandlePaid(payload.Data, cancellationToken);
                    break;
                case "checkout.failed":
                    await HandleFailed(payload.Data, cancellationToken);
                    break;
                case "checkout.expired":
                    await HandleFailed(payload.Data, cancellationToken);
                    break;
                default:
                    await HandleFailed(payload.Data, cancellationToken);
                    break;
                    // Ignore all other event types
            }
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        private async Task HandlePaid(
            ChargilyCheckoutData data,
            CancellationToken ct)
        {
            if (!TryGetPaymentId(data, out var paymentId)) return;

            var payment = await db.SubscriptionPayments
                .FirstOrDefaultAsync(e => e.Id == paymentId, ct);

            if (payment is null || payment.Status == PaymentStatus.Paid)
                return; // already processed — idempotent

            var subscription = await db.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId, ct);

            if (subscription is null) return;

            payment.MarkPaid(JsonSerializer.Serialize(data));
            subscription.Activate();
            logger.LogInformation("Payment {PaymentId} marked as paid and subscription {SubscriptionId} activated",
                payment.Id, subscription.Id);

        }

        private async Task HandleFailed(
            ChargilyCheckoutData data,
            CancellationToken ct)
        {
            if (!TryGetPaymentId(data, out var paymentId)) return;

            var payment = await db.SubscriptionPayments
                .Include(e => e.Subscription)
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

            if (payment is null || payment.Status == PaymentStatus.Failed)
                return; // idempotent

            payment.MarkFailed(data.FailureReason ?? "Payment failed",
                JsonSerializer.Serialize(data));
            payment.Subscription.PaymentFailed();

            logger.LogInformation("Payment {PaymentId} marked as failed. Reason: {FailureReason}",
                payment.Id, data.FailureReason);
        }

        private static bool TryGetPaymentId(
            ChargilyCheckoutData data,
            out Guid paymentId)
        {
            paymentId = Guid.Empty;
            var paymentInfo = data.Metadata?
                .ElementAt(0)
                .Split(':', 2);
            var id = paymentInfo?.ElementAt(1);
            return Guid.TryParse(id, out paymentId);

        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/payments/webhook/chargily", async (
            HttpRequest httpRequest,
            ICommandHandler<Command> handler,
            CancellationToken ct) =>
        {
            // Read raw body — needed to forward to handler
            using var reader = new StreamReader(httpRequest.Body);
            var rawBody = await reader.ReadToEndAsync(ct);


            await handler.Handle(new Command(rawBody), ct);

            // Always return 200 — Chargily retries on non-2xx
            return Results.Ok();
        })
        .WithTags("Payments")
        .AllowAnonymous();
        }
    }
}
