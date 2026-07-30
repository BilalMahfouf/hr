using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Domain.Subscriptions.Errors;

namespace PublicApi.Features.SubscriptionPlans;

public static class DeactivateSubscriptionPlan
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
            subscriptionPlan.Deactivate();
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/subscription-plans/{id:guid}/deactivate", async (
                Guid id,
                ICommandHandler<Command> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new Command(id), ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            }).RequireAuthorization()
            .WithTags("Subscription Plans")
            .WithSummary("Deactivate subscription plan")
            .WithDescription("Deactivates a subscription plan to prevent new subscriptions from using it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("DeactivateSubscriptionPlan");
        }
    }
}
