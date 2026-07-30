using Microsoft.EntityFrameworkCore;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using Shared.CQRS;
using Shared.Endpoints;
using Shared.Results;
using VeterinaryApi.Domain.Subscriptions.Errors;

namespace VeterinaryApi.Features.SubscriptionPlans;

public static class ActivateSubscriptionPlan
{
    public sealed record Command(Guid Id) : ICommand;

    public sealed class CommandHandler(IApplicationDbContext db)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken = default)
        {
            var subscriptionPlan = await db.SubscriptionPlans
                .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);
            if (subscriptionPlan is null)
            {
                return Result.Failure(SubscriptionPlanErrors.SubscriptionPlanNotFound());
            }
            subscriptionPlan.Activate();
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/subscription-plans/{id:guid}/activate", async (
                Guid id,
                ICommandHandler<Command> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new Command(id), ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            }).RequireAuthorization()
            .WithTags("Subscription Plans")
            .WithSummary("Activate subscription plan")
            .WithDescription("Activates a subscription plan so it becomes available for new subscriptions.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("ActivateSubscriptionPlan");
        }
    }
}
