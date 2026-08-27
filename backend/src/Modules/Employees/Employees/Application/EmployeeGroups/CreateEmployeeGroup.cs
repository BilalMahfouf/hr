using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups;

public static class CreateEmployeeGroup
{
    public sealed class Validator : AbstractValidator<CreateEmployeeGroupCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.RotationStartDate)
                .NotEqual(default(DateOnly))
                .WithMessage("Rotation start date is required.");

            RuleForEach(x => x.WorkSchedules)
                .SetValidator(new WorkScheduleRequestValidator());

            RuleForEach(x => x.RotationEntries)
                .Must(r => r.Position >= 1)
                .WithMessage("Rotation position must be greater than or equal to 1.");

            RuleFor(x => x.RotationEntries)
                .Must(entries => entries.Select(e => e.Position).Distinct().Count() == entries.Count)
                .WithMessage("Rotation positions must be unique.")
                .When(x => x.RotationEntries.Count > 0);

            RuleFor(x => x.RotationEntries)
                .NotEmpty()
                .WithMessage("At least one rotation entry is required.");

            RuleFor(x => x)
                .Must(ValidateScheduleReferences)
                .WithMessage("Rotation entries reference work schedules that don't exist in the request.");

            RuleFor(x => x)
                .Must(c => c.WorkSchedules.Count > 0 || c.RotationEntries.All(r => r.WorkScheduleIndex is null))
                .WithMessage("Rotation entries cannot reference work schedules when none are provided.");
        }

        private static bool ValidateScheduleReferences(CreateEmployeeGroupCommand c)
        {
            foreach (var entry in c.RotationEntries)
            {
                if (entry.WorkScheduleIndex.HasValue)
                {
                    if (entry.WorkScheduleIndex.Value < 0 || entry.WorkScheduleIndex.Value >= c.WorkSchedules.Count)
                        return false;
                }
            }
            return true;
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<CreateEmployeeGroupCommand> validator)
        : ICommandHandler<CreateEmployeeGroupCommand, EmployeeGroupResponse>
    {
        public async Task<Result<EmployeeGroupResponse>> Handle(
            CreateEmployeeGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var existing = await repository.GetByNameAsync(command.Name, cancellationToken);
            if (existing is not null)
            {
                return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.NameAlreadyExists);
            }

            var group = EmployeeGroup.Create(
                command.Name,
                command.IsSecurity,
                command.RotationStartDate,
                command.Description);

            group.ReplaceSchedulesAndRotations(
                command.WorkSchedules.Select(r => r.ToDto(group.Id)).ToList(),
                command.RotationEntries
                    .Select(r => (r.Position, r.WorkScheduleIndex))
                    .ToList());

            repository.Add(group);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<EmployeeGroupResponse>.Success(EmployeeGroupMapper.ToResponse(group));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("employee-groups", async (
                CreateEmployeeGroupRequest request,
                ICommandHandler<CreateEmployeeGroupCommand, EmployeeGroupResponse> handler,
                CancellationToken ct) =>
            {
                var command = new CreateEmployeeGroupCommand(
                    request.Name,
                    request.IsSecurity,
                    request.Description,
                    request.RotationStartDate,
                    request.WorkSchedules,
                    request.RotationEntries);

                var result = await handler.Handle(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/v1/employee-groups/{result.Value.Id}", result.Value)
                    : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Create employee group with schedules and rotations")
            .WithDescription("Creates a new employee group with work schedules and rotation entries in a single atomic transaction. Rotation entries reference schedules by their index in the workSchedules array.")
            .Produces<EmployeeGroupResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateEmployeeGroup");
        }
    }
}