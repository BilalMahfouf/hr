using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups.Rotations;

public static class CreateRestRotation
{
    public sealed class Validator : AbstractValidator<CreateRestRotationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Position)
                .GreaterThanOrEqualTo(1);
        }
    }

    public sealed class Handler(
        IEmployeeDbContext dbContext,
        IValidator<CreateRestRotationCommand> validator)
        : ICommandHandler<CreateRestRotationCommand, RotationEntryResponse>
    {
        public async Task<Result<RotationEntryResponse>> Handle(
            CreateRestRotationCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await dbContext.EmployeeGroups
                .Include(g => g.RotationEntries)
                .FirstOrDefaultAsync(g => g.Id == new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            if (group.RotationEntries.Any(re => re.Position == command.Position))
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.DuplicateRotationPosition);
            }

            group.AddRotationEntry(command.Position, null);

            await dbContext.SaveChangesAsync(cancellationToken);

            var entry = group.RotationEntries.First(re => re.Position == command.Position);
            return Result<RotationEntryResponse>.Success(EmployeeGroupMapper.ToResponse(entry));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("employee-groups/{groupId:guid}/rotations/rest", async (
                Guid groupId,
                CreateRestRotationRequest request,
                ICommandHandler<CreateRestRotationCommand, RotationEntryResponse> handler,
                CancellationToken ct) =>
            {
                var command = new CreateRestRotationCommand(groupId, request.Position);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/v1/employee-groups/{groupId}/rotations/{request.Position}", result.Value)
                    : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Create rest rotation entry")
            .WithDescription("Adds a rest rotation entry (no work schedule) at the specified position.")
            .Produces<RotationEntryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateRestRotation");
        }
    }
}