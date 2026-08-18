using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Punches;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.Importer;

public static class ImportAttendanceLogs
{
    public sealed record Command(
        DateOnly From,
        DateOnly To) : ICommand<Response>;

    public sealed record Response(
        int MachineCount,
        int PunchCount);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.From)
                .LessThanOrEqualTo(x => x.To);
        }
    }

    public sealed class CommandHandler(
        IAttendanceDbContext db,
        IAttendanceMachineReader reader,
        IValidator<Command> validator,
        ILogger<CommandHandler> logger)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

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
                .Where(p => p.PunchOccurredAt >= from && p.PunchOccurredAt < toExclusive)
                .Select(p => new { p.MachineId, p.EmployeeBadge, p.PunchOccurredAt })
                .ToListAsync(cancellationToken);

            var seen = existing
                .Select(x => (x.MachineId, x.EmployeeBadge, x.PunchOccurredAt))
                .ToHashSet();

            var punchCount = 0;

            foreach (var machine in machines)
            {
                IReadOnlyList<RawAttendanceLog> logs;

                try
                {
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

                foreach (var log in logs)
                {
                    if (!int.TryParse(log.EmployeeNumber, out var badge) || badge <= 0)
                    {
                        logger.LogWarning(
                            "Skipping log with invalid employee number " +
                            "'{EmployeeNumber}' on machine {MachineNumber}",
                            log.EmployeeNumber,
                            machine.MachineNumber);
                        continue;
                    }

                    var timestamp = DateTime.SpecifyKind(log.Timestamp, DateTimeKind.Utc);

                    if (!seen.Add((log.MachineId, badge, timestamp)))
                        continue;

                    db.Punches.Add(Punch.Create(
                        log.MachineId,
                        badge,
                        timestamp,
                        DateTime.UtcNow));

                    punchCount++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                machines.Count,
                punchCount));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {

            app.MapPost("attendance/import", async (
                Command command,
                ICommandHandler<ImportAttendanceLogs.Command, ImportAttendanceLogs.Response> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value)
                : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Import attendance logs from the machines")
            .WithDescription("Reads attendance logs from all active machines and persists them as punches.");
        }
    }
}