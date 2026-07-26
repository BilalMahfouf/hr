using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions.Errors;

namespace VeterinaryApi.Features.SubscriptionPlans;

public static class UpdateSubscriptionPlan
{
    public sealed record Request(
         string Name,
        decimal Amount,
        string Currency,
        string BillingInterval,
        int IntervalCount,
        int TrialDays);
    public sealed record Command(
        Guid Id,
        string Name,
        decimal Amount,
        string Currency,
        string BillingInterval,
        int IntervalCount,
        int TrialDays) : ICommand<Response>;
    public sealed record Response(Guid Id);


    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(e => e.Id)
                .NotEmpty();
            RuleFor(e => e.Name)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(e => e.Amount)
                .GreaterThanOrEqualTo(0);
            RuleFor(e => e.Currency)
                .NotEmpty()
                .Length(3);
            RuleFor(e => e.BillingInterval)
                .NotEmpty()
                .Must(e => new[] { "day", "week", "month", "year" }
                .Contains(e.ToLower()))
                .WithMessage("BillingInterval must be one of the following values: day, week, month, year.");
            RuleFor(e => e.IntervalCount)
                .GreaterThan(0);
            RuleFor(e => e.TrialDays)
                .GreaterThanOrEqualTo(0);
        }
    }

    public sealed class CommandHandler(
        IApplicationDbContext db,
        IValidator<Command> validator)
       : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);
            var existingPlan = await db.SubscriptionPlans
                    .FirstOrDefaultAsync(
                    e => e.Id == command.Id, cancellationToken);
            if (existingPlan is null)
            {
                return Result<Response>.Failure(SubscriptionPlanErrors
                    .SubscriptionPlanNotFound());
            }
            var price = new Money(command.Amount, command.Currency);
            existingPlan.Update(
                command.Name,
                price,
                command.BillingInterval,
                command.IntervalCount,
                command.TrialDays);

            await db.SaveChangesAsync(cancellationToken);
            return Result<Response>.Success(new Response(existingPlan.Id));
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/subscription-plans/{id:guid}", async (
                Guid id,
                Request request,
                ICommandHandler<Command, Response> handler,
                CancellationToken ct) =>
            {
                var command = new Command(
                    id,
                    request.Name,
                    request.Amount,
                    request.Currency,
                    request.BillingInterval,
                    request.IntervalCount,
                    request.TrialDays);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : result.Problem();
            }).RequireAuthorization()
            .WithTags("Subscription Plans")
            .WithSummary("Update subscription plan")
            .WithDescription("Updates an existing subscription plan by its unique identifier.")
            .Produces<Response>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("UpdateSubscriptionPlan");
        }
    }
}
