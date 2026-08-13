using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Application.AttendenceRerords;

public static class CreateAttendenceRecord
{
    public sealed record Command(
        int EmployeeBadge,
        MachineId MachineId,
        DateTime PunchOccurredAt
       ) : ICommand;

    public sealed class CommandHandler(IEmployeeApi employeeApi, IAttendanceDbContext db) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken = default)
        {
            var employee = await employeeApi.GetEmployeeByBadgeAsync(command.EmployeeBadge, cancellationToken);
            if (!employee.IsSuccess)
        {
                return Result.Failure(EmployeeErrors.NotFound);
            }
            // if there is record for today with checkout null set punch as checkout.
            // if now create new record .
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var attendenceRecord = await db.AttendanceRecords
                .Where(e =>
                e.EmployeeId == employee.Value.EmployeeId &&
                e.CreatedOnUtc >= today &&
                e.CreatedOnUtc < tomorrow)
                .OrderByDescending(e => e.CreatedOnUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (attendenceRecord is null || attendenceRecord.CheckOutAt.HasValue)
            {
                var newRecord = AttendanceRecord.Create(command.MachineId, employee.Value.EmployeeId, command.PunchOccurredAt);
                newRecord.RegisterCheckIn(command.PunchOccurredAt, employee.Value.Schedule.ExpectedCheckInTime);
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success;
            }

            var schedule = new Domain.AttendenceRecords
                .WorkSchedule(employee.Value.Schedule.StandardWorkTime, employee.Value.Schedule.ExpectedCheckOutTime);

            attendenceRecord.RegisterCheckOut(command.PunchOccurredAt, schedule);

            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }
}
