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

public static class DeleteRotation
{
    public sealed class Handler(IEmployeeDbContext dbContext)
        : ICommandHandler<DeleteRotationCommand>
    {
        public async Task<Result> Handle(
            DeleteRotationCommand command,
            CancellationToken cancellationToken = default)
        {
            var group = await dbContext.EmployeeGroups
                .Include(g => g.RotationEntries)
                .FirstOrDefaultAsync(g => g.Id == new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result.Failure(EmployeeGroupErrors.NotFound);
            }

            var entry = group.RotationEntries
                .FirstOrDefault(re => re.Position == command.Position);
            if (entry is null)
            {
                return Result.Failure(EmployeeGroupErrors.RotationEntryNotFound);
            }

            group.RemoveRotationEntry(command.Position);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("employee-groups/{groupId:guid}/rotations/{position:int}", async (
                Guid groupId,
                int position,
                ICommandHandler<DeleteRotationCommand> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new DeleteRotationCommand(groupId, position), ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Delete rotation entry")
            .WithDescription("Deletes a rotation entry at the specified position.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("DeleteRotation");
        }
    }
}