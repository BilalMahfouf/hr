using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Subscriptions.Errors;

namespace VeterinaryApi.Features.Subscriptions.Endpoints;

public static class Me
{
    public sealed record Query(Guid DoctorId) : IQuery<Response>;
    public sealed record Response(
        Guid Id,
    Guid DoctorId,
    Guid PlanId,
    string PlanName,
    string PlanDisplayName,
    decimal PlanPrice,
    string PlanCurrency,
    string SubscriptionStatus,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    DateTime? CancelledAt,
    DateTime? UpdatedAt,
    Guid? PreviousSubscriptionId);

    public sealed class QueryHandler(IApplicationDbContext db)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var subscription = await db.Subscriptions
                .Where(e => e.DoctorId == query.DoctorId)
                .OrderByDescending(e => e.CreatedOnUtc)
                .Select(e => new Response(
                    e.Id,
                    e.DoctorId,
                    e.PlanId,
                    e.Plan.Name,
                    e.Plan.Slug,
                    e.Plan.Price.Amount,
                    e.Plan.Price.Currency,
                    e.Status.ToString(),
                    e.CurrentPeriodStart,
                    e.CurrentPeriodEnd,
                    e.TrialEndsAt,
                    e.CancelledAt,
                    e.UpdatedAt,
                    e.PreviousSubscriptionId))
                .FirstOrDefaultAsync(cancellationToken);
            if (subscription is null)
            {
                return Result<Response>.Failure(SubscriptionErrors.NotFound);
            }
            return Result<Response>.Success(subscription);
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/subscriptions/me", async (
                ICurrentTenant currentTenant,
                IQueryHandler<Query, Response> handler,
                CancellationToken ct) =>
            {
                var query = new Query(currentTenant.UserId!.Value);
                var result = await handler.Handle(query, ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                     : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(Subscription)}s")
            .WithSummary("Get current subscription")
            .WithDescription("Retrieves the latest subscription details for the currently authenticated doctor.")
            .Produces<Response>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("GetMySubscription");
        }
    }
}
