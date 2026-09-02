using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Domain.Punches;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.Importer;

public static class ImportAttendanceByEmployee
{
    public sealed record Response(
        int MachineCount,
        int PunchCount);

    // ────────────────────────────────────────────
    //  Endpoint A — Import by Employee
    // ────────────────────────────────────────────

    public sealed record ImportByEmployeeCommand(
        string EmployeeId,
        DateOnly From,
        DateOnly To) : ICommand<Response>;

    public sealed class ImportByEmployeeValidator
        : AbstractValidator<ImportByEmployeeCommand>
    {
        public ImportByEmployeeValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty();

            RuleFor(x => x.From)
                .LessThanOrEqualTo(x => x.To);
        }
    }

    public sealed class ImportByEmployeeHandler(
        IAttendanceDbContext db,
        IAttendanceMachineReaderFactory readerFactory,
        IEmployeeApi employeeApi,
        IValidator<ImportByEmployeeCommand> validator,
        ILogger<ImportByEmployeeHandler> logger)
        : ICommandHandler<ImportByEmployeeCommand, Response>
    {
        public async Task<Result<Response>> Handle(
            ImportByEmployeeCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var employeeResult = await employeeApi.GetEmployeeByIdAsync(
                command.EmployeeId, cancellationToken);

            if (!employeeResult.IsSuccess)
            {
                return Result<Response>.Failure(EmployeeErrors.NotFound);
            }

            var badge = employeeResult.Value.Bgd;

            var machines = await db.Machines
                .Where(m => m.IsActive)
                .ToListAsync(cancellationToken);

            var from = DateTime.SpecifyKind(
                command.From.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            var toExclusive = DateTime.SpecifyKind(
                command.To.AddDays(1).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);

            var existing = await db.Punches
                .Where(p =>
                    p.PunchOccurredAt >= from &&
                    p.PunchOccurredAt < toExclusive)
                .Select(p => new
                {
                    p.MachineId,
                    p.EmployeeBadge,
                    p.PunchOccurredAt
                })
                .ToListAsync(cancellationToken);

            var seen = existing
                .Select(x => (x.MachineId, x.EmployeeBadge, x.PunchOccurredAt))
                .ToHashSet();

            var punchCount = 0;
            var machinesWithPunches = 0;

            List<Punch> punches = new();

            foreach (var machine in machines)
            {
                IReadOnlyList<RawAttendanceLog> logs;

                try
                {
                    var reader = readerFactory.Create(machine);

                    logs = await reader.GetLogsAsync(
                        machine,
                        command.From,
                        command.To,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to read logs from attendance machine " +
                        "{MachineNumber} ({IpAddress}:{Port})",
                        machine.MachineNumber,
                        machine.IpAddress,
                        machine.Port);
                    continue;
                }

                var machineHadPunches = false;

                foreach (var log in logs)
                {
                    if (!int.TryParse(log.EmployeeNumber, out var logBadge) ||
                        logBadge != badge)
                    {
                        continue;
                    }

                    var timestamp = DateTime.SpecifyKind(
                        log.Timestamp, DateTimeKind.Utc);

                    if (!seen.Add((log.MachineId, logBadge, timestamp)))
                        continue;

                    var punch = Punch.Create(
                        log.MachineId,
                        logBadge,
                        timestamp,
                        DateTime.UtcNow);
                    punches.Add(punch);

                    punchCount++;
                    machineHadPunches = true;
                }

                if (machineHadPunches)
                {
                    machinesWithPunches++;
                }
            }

            var uniquePunches = punches.DistinctBy(e => new
            {
                e.MachineId,
                e.EmployeeBadge,
                e.PunchOccurredAt
            }).ToList();

            db.Punches.AddRange(uniquePunches);
            await db.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(
                new Response(machinesWithPunches, punchCount));
        }
    }

    public sealed class ImportByEmployeeEndpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost(
                "attendance/import/employee/{employeeId}",
                async (
                    string employeeId,
                    ImportByEmployeeCommand command,
                    ICommandHandler<ImportByEmployeeCommand, Response> handler
                    ) =>
                {
                    var cmd = command with { EmployeeId = employeeId };
                    var result = await handler.Handle(cmd, CancellationToken.None);
                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Problem();
                })
            .WithTags("Attendance")
            .WithSummary("Import attendance logs for a specific employee")
            .WithDescription(
                "Reads attendance logs from all active machines " +
                "and persists punches for the specified employee.");
        }
    }

    // ────────────────────────────────────────────
    //  Endpoint B — Import by Machine
    // ────────────────────────────────────────────

    public sealed record ImportByMachineCommand(
        Guid MachineId,
        DateOnly From,
        DateOnly To) : ICommand<Response>;

    public sealed class ImportByMachineValidator
        : AbstractValidator<ImportByMachineCommand>
    {
        public ImportByMachineValidator()
        {
            RuleFor(x => x.MachineId)
                .NotEmpty();

            RuleFor(x => x.From)
                .LessThanOrEqualTo(x => x.To);
        }
    }

    public sealed class ImportByMachineHandler(
        IAttendanceDbContext db,
        IAttendanceMachineReaderFactory readerFactory,
        IValidator<ImportByMachineCommand> validator,
        ILogger<ImportByMachineHandler> logger)
        : ICommandHandler<ImportByMachineCommand, Response>
    {
        public async Task<Result<Response>> Handle(
            ImportByMachineCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var machineId = MachineId.From(command.MachineId);

            var machine = await db.Machines
                .FirstOrDefaultAsync(
                    m => m.Id == machineId, cancellationToken);

            if (machine is null)
            {
                return Result<Response>.Failure(
                    MachineErrors.MachineNotFound(command.MachineId));
            }

            var from = DateTime.SpecifyKind(
                command.From.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            var toExclusive = DateTime.SpecifyKind(
                command.To.AddDays(1).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);

            var existing = await db.Punches
                .Where(p =>
                    p.MachineId == machineId &&
                    p.PunchOccurredAt >= from &&
                    p.PunchOccurredAt < toExclusive)
                .Select(p => new
                {
                    p.MachineId,
                    p.EmployeeBadge,
                    p.PunchOccurredAt
                })
                .ToListAsync(cancellationToken);

            var seen = existing
                .Select(x => (x.MachineId, x.EmployeeBadge, x.PunchOccurredAt))
                .ToHashSet();

            var punchCount = 0;

            IReadOnlyList<RawAttendanceLog> logs;

            try
            {
                var reader = readerFactory.Create(machine);

                logs = await reader.GetLogsAsync(
                    machine,
                    command.From,
                    command.To,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read logs from attendance machine " +
                    "{MachineNumber} ({IpAddress}:{Port})",
                    machine.MachineNumber,
                    machine.IpAddress,
                    machine.Port);

                return Result<Response>.Success(new Response(0, 0));
            }

            List<Punch> punches = new();

            foreach (var log in logs)
            {
                if (!int.TryParse(log.EmployeeNumber, out var badge) ||
                    badge <= 0)
                {
                    logger.LogWarning(
                        "Skipping log with invalid employee number " +
                        "'{EmployeeNumber}' on machine {MachineNumber}",
                        log.EmployeeNumber,
                        machine.MachineNumber);
                    continue;
                }

                var timestamp = DateTime.SpecifyKind(
                    log.Timestamp, DateTimeKind.Utc);

                if (!seen.Add((log.MachineId, badge, timestamp)))
                    continue;

                var punch = Punch.Create(
                    log.MachineId,
                    badge,
                    timestamp,
                    DateTime.UtcNow);
                punches.Add(punch);

                punchCount++;
            }

            var uniquePunches = punches.DistinctBy(e => new
            {
                e.MachineId,
                e.EmployeeBadge,
                e.PunchOccurredAt
            }).ToList();

            db.Punches.AddRange(uniquePunches);
            await db.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(new Response(1, punchCount));
        }
    }

    public sealed class ImportByMachineEndpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost(
                "attendance/import/machine/{machineId:guid}",
                async (
                    Guid machineId,
                    ImportByMachineCommand command,
                    ICommandHandler<ImportByMachineCommand, Response> handler,
                    CancellationToken cancellationToken) =>
                {
                    var cmd = command with { MachineId = machineId };
                    var result = await handler.Handle(cmd, cancellationToken);
                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Problem();
                })
            .WithTags("Attendance")
            .WithSummary("Import attendance logs from a specific machine")
            .WithDescription(
                "Reads attendance logs from the specified machine " +
                "and persists all punches.");
        }
    }
}
