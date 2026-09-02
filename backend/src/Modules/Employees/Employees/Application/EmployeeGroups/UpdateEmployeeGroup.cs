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

namespace Modules.Employees.Application.EmployeeGroups;

public static class UpdateEmployeeGroup
{
    public sealed class Validator : AbstractValidator<UpdateEmployeeGroupCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100)
                .When(x => x.Name is not null);
        }
    }

    public sealed class Handler(
        IEmployeeDbContext dbContext,
        IValidator<UpdateEmployeeGroupCommand> validator)
        : ICommandHandler<UpdateEmployeeGroupCommand, EmployeeGroupResponse>
    {
        public async Task<Result<EmployeeGroupResponse>> Handle(
            UpdateEmployeeGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await dbContext.EmployeeGroups
                .Include(g => g.WorkSchedules)
                .Include(g => g.RotationEntries)
                    .ThenInclude(re => re.WorkSchedule)
                .FirstOrDefaultAsync(g => g.Id == new EmployeeGroupId(command.Id), cancellationToken);
            if (group is null)
            {
                return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            if (command.Name is not null && !command.Name.Equals(group.Name, StringComparison.OrdinalIgnoreCase))
            {
                var nameTaken = await dbContext.EmployeeGroups
                    .AnyAsync(g => g.Name == command.Name, cancellationToken);
                if (nameTaken)
                {
                    return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.NameAlreadyExists);
                }
            }

            group.UpdateDetails(command.Name, command.IsSecurity, command.Description);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<EmployeeGroupResponse>.Success(EmployeeGroupMapper.ToResponse(group));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("employee-groups/{id:guid}", async (
                Guid id,
                UpdateEmployeeGroupRequest request,
                ICommandHandler<UpdateEmployeeGroupCommand, EmployeeGroupResponse> handler,
                CancellationToken ct) =>
            {
                var command = new UpdateEmployeeGroupCommand(id, request.Name, request.IsSecurity, request.Description);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Update employee group metadata")
            .WithDescription("Updates employee group name, security flag, or description. Does not modify schedules or rotations.")
            .Produces<EmployeeGroupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("UpdateEmployeeGroup");
        }
    }
}