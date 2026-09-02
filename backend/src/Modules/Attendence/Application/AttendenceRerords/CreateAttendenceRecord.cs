using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Domain.Common;
using Modules.Shared.Results;
using Modules.Shared.Util;
using System;
using System.Collections.Generic;

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
            var employee = await employeeApi.GetEmployeeForAttendance(
                command.EmployeeBadge,
                DateOnly.FromDateTime(command.PunchOccurredAt),
                cancellationToken);
            if (!employee.IsSuccess)
            {
                return Result.Failure(EmployeeErrors.NotFound);
            }
            if (employee.Value.WorkStatus == EmployeeWorkStatus.Rest)
            {
                return await HandleRestDay(command, employee.Value, cancellationToken);
            }

            var punches = await db.Punches
               .Where(e => e.EmployeeBadge == command.EmployeeBadge &&
                       e.PunchOccurredAt.Date >= employee.Value.ShiftStartDateTime.ToUtc().Date &&
                       e.PunchOccurredAt.Date <= employee.Value.ShiftEndDateTime.ToUtc().Date)
               .OrderBy(e => e.PunchOccurredAt)
               .ToListAsync(cancellationToken);

            var attendanceRecords = await db.AttendanceRecords
                .Where(e => e.EmployeeId == employee.Value.EmployeeId &&
                        e.CheckInAt.Date >= employee.Value.ShiftStartDateTime.ToUtc().Date &&
                        e.CheckInAt.Date <= employee.Value.ShiftEndDateTime.ToUtc().Date)
                .OrderBy(e => e.CheckInAt)
                .ToListAsync(cancellationToken);
            if (attendanceRecords.Any())
            {
                db.AttendanceRecords.RemoveRange(attendanceRecords);
            }

            var newAttendanceRecords = new List<AttendanceRecord>();
            foreach (var punch in punches)
            {
                var lastRecord = newAttendanceRecords
                    .OrderBy(e => e.CheckInAt)
                    .LastOrDefault();
                if (lastRecord is null || lastRecord.CheckOutAt.HasValue)
                {
                    var newRecord = AttendanceRecord.Create(command.MachineId, employee.Value.EmployeeId);
                    try
                    {
                        newRecord.RegisterCheckIn(punch.PunchOccurredAt,
                                                   employee.Value.ShiftStartDateTime.ToUtc(),
                                                   lastRecord);
                    }
                    catch (DomainException)
                    {
                        continue;
                    }
                    newAttendanceRecords.Add(newRecord);
                    continue;
                }
                var schedule = new Modules.Attendence.Domain.AttendenceRecords.WorkSchedule(
                             employee.Value.WorkTime,
                            employee.Value.ShiftEndDateTime.ToUtc());

                try
                {
                    lastRecord.RegisterCheckOut(punch.PunchOccurredAt, schedule);
                }
                catch (DomainException)
                {
                    continue;
                }
            }
            db.AttendanceRecords.AddRange(newAttendanceRecords);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        private async Task<Result> HandleRestDay(Command command, EmployeeReponseForAttendance employee, CancellationToken cancellationToken)
        {
            var punches = await db.Punches
                   .Where(e => e.EmployeeBadge == command.EmployeeBadge &&
                           e.PunchOccurredAt.Date == command.PunchOccurredAt.ToUtc().Date)
                   .OrderBy(e => e.PunchOccurredAt)
                   .ToListAsync(cancellationToken);

            var attendanceRecords = await db.AttendanceRecords
                .Where(e => e.EmployeeId == employee.EmployeeId &&
                        e.CheckInAt.Date == command.PunchOccurredAt.ToUtc().Date)
                .OrderBy(e => e.CheckInAt)
                .ToListAsync(cancellationToken);
            foreach (var punch in punches)
            {
                var lastRecord = attendanceRecords
                   .OrderBy(e => e.CheckInAt)
                   .LastOrDefault();
                if (lastRecord is null || lastRecord.CheckOutAt.HasValue)
                {
                    var newRecord = AttendanceRecord.Create(command.MachineId, employee.EmployeeId);
                    try
                    {
                        newRecord.RegisterCheckIn(punch.PunchOccurredAt,
                                                   employee.ShiftEndDateTime.ToUtc(),
                                                   lastRecord);
                    }
                    catch (DomainException)
                    {
                        continue;
                    }
                    attendanceRecords.Add(newRecord);
                    continue;
                }
                var schedule = new Modules.Attendence.Domain.AttendenceRecords.WorkSchedule(
                             employee.WorkTime,
                            employee.ShiftEndDateTime);

                try
                {
                    lastRecord.RegisterCheckOut(punch.PunchOccurredAt, schedule);
                }
                catch (DomainException)
                {
                    continue;
                }
            }
            db.AttendanceRecords.AddRange(attendanceRecords);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }


    }
}
