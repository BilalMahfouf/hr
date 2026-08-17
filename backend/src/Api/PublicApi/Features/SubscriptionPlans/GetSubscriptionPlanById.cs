using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Domain.Subscriptions.Errors;

namespace PublicApi.Features.SubscriptionPlans;

public static class GetSubscriptionPlanById
{
    public sealed record Query(Guid Id) : IQuery<Shared.Response>;

    public sealed class QueryHandler(IApplicationDbContext db)
        : IQueryHandler<Query, Shared.Response>
    {
        public async Task<Result<Shared.Response>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var plan = await db.SubscriptionPlans
                .AsNoTracking()
                .Where(e => e.Id == query.Id)
                .Select(e => new Shared.Response(
                    e.Id,
                    e.Name,
                    e.Slug,
                    e.Price.Amount,
                    e.Price.Currency,
                    e.BillingInterval,
                    e.IntervalCount,
                    e.TrialDays,
                    e.IsActive,
                    e.CreatedOnUtc))
                .FirstOrDefaultAsync(cancellationToken);
            if (plan is null)
            {
                return Result<Shared.Response>.Failure(SubscriptionPlanErrors
                    .SubscriptionPlanNotFound());
            }
            return Result<Shared.Response>.Success(plan);
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/subscription-plans/{id:guid}", async (
                Guid id,
                IQueryHandler<Query, Shared.Response> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new Query(id), ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : result.Problem();

            }).RequireAuthorization()
            .WithTags("Subscription Plans")
            .WithSummary("Get subscription plan by ID")
            .WithDescription("Retrieves a subscription plan by its unique identifier.")
            .Produces<Shared.Response>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("GetSubscriptionPlanById");
        }
    }
}
