using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Domain.Subscriptions;
using PublicApi.Domain.Subscriptions.Errors;

namespace PublicApi.Features.SubscriptionPlans;

public static class GetAllSubscriptionPlans
{
    public sealed record Response(
     Guid Id,
    string Name,
    string Slug,
    decimal Amount,
    string Currency,
    string BillingInterval,
    int IntervalCount,
    int TrialDays,
    bool IsActive,
    DateTime CreatedOnUtc);

    public sealed record Query() : IQuery<IEnumerable<Response>>;

    public sealed class QueryHandler(IApplicationDbContext db)
        : IQueryHandler<Query, IEnumerable<Response>>
    {
        public async Task<Result<IEnumerable<Response>>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var plans = await db.SubscriptionPlans
                .AsNoTracking()
                .Select(e => new Response(
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
                .ToListAsync(cancellationToken);
            if (plans is null || !plans.Any())
            {
                return Result<IEnumerable<Response>>.Failure(SubscriptionPlanErrors
                    .SubscriptionPlansNotFound);
            }
            return Result<IEnumerable<Response>>.Success(plans);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("subscription-plans", async (
                IQueryHandler<Query, IEnumerable<Response>> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new Query(), ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(SubscriptionPlan)}s")
            .WithSummary("Get all subscription plans")
            .WithDescription("Retrieves all available subscription plans.")
            .Produces<IEnumerable<Response>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("GetAllSubscriptionPlans");
        }
    }
}
