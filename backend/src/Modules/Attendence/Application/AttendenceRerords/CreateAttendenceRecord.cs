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
            // if there is record for the punch's day with checkout null set punch as checkout.
            // if not, create new record .
            var punchDate = command.PunchOccurredAt.Date;
            var nextDay = punchDate.AddDays(1);

            var attendenceRecord = await db.AttendanceRecords
                .Where(e =>
                e.EmployeeId == employee.Value.EmployeeId &&
                e.CheckInAt >= punchDate &&
                e.CheckInAt < nextDay)
                .OrderByDescending(e => e.CreatedOnUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (attendenceRecord is null || attendenceRecord.CheckOutAt.HasValue)
            {
                var newRecord = AttendanceRecord.Create(command.MachineId, employee.Value.EmployeeId);
                newRecord.RegisterCheckIn(command.PunchOccurredAt, employee.Value.Schedule.ExpectedCheckInTime);
                db.AttendanceRecords.Add(newRecord);
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success;
            }

            var schedule = new Domain.AttendenceRecords
                .WorkSchedule(employee.Value.Schedule.StandardWorkTime, employee.Value.Schedule.ExpectedCheckOutTime);

            attendenceRecord.RegisterCheckOut(command.PunchOccurredAt, schedule);

            db.AttendanceRecords.Update(attendenceRecord);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }
}
