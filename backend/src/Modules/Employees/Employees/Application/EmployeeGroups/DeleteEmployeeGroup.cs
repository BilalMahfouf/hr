using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups;

public static class DeleteEmployeeGroup
{
    public sealed class Handler(IEmployeeDbContext dbContext)
        : ICommandHandler<DeleteEmployeeGroupCommand>
    {
        public async Task<Result> Handle(
            DeleteEmployeeGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            var group = await dbContext.EmployeeGroups
                .FirstOrDefaultAsync(g => g.Id == new EmployeeGroupId(command.Id), cancellationToken);
            if (group is null)
            {
                return Result.Failure(EmployeeGroupErrors.NotFound);
            }

            dbContext.EmployeeGroups.Remove(group);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("employee-groups/{id:guid}", async (
                Guid id,
                ICommandHandler<DeleteEmployeeGroupCommand> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new DeleteEmployeeGroupCommand(id), ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Delete employee group")
            .WithDescription("Deletes an employee group and all its associated work schedules and rotation entries.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("DeleteEmployeeGroup");
        }
    }
}