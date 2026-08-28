using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Domain.Common;
using Modules.Shared.Results;
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
            var employee = await employeeApi.GetEmployeeByBadgeAsync(
                command.EmployeeBadge,
                DateOnly.FromDateTime(command.PunchOccurredAt),
                cancellationToken);
            if (!employee.IsSuccess)
            {
                return Result.Failure(EmployeeErrors.NotFound);
            }
            if (employee.Value.Schedule.WorkStatus == EmployeeWorkStatus.Rest)
            {
                return await HandleRestDay(command, employee.Value, cancellationToken);
            }
            var workSchedule = employee.Value.Schedule;

            var punches = await db.Punches
               .Where(e => e.EmployeeBadge == command.EmployeeBadge &&
                       e.PunchOccurredAt.Date >= workSchedule.ShiftStartDateTime.Date &&
                       e.PunchOccurredAt.Date <= workSchedule.ShiftEndtDateTime.Date)
               .OrderBy(e => e.PunchOccurredAt)
               .ToListAsync(cancellationToken);

            var attendanceRecords = await db.AttendanceRecords
                .Where(e => e.EmployeeId == employee.Value.EmployeeId &&
                        e.CheckInAt.Date >= workSchedule.ShiftStartDateTime.Date &&
                        e.CheckInAt.Date <= workSchedule.ShiftEndtDateTime.Date)
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
                                                   workSchedule.ShiftStartDateTime,
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
                             workSchedule.WorkTime,
                            workSchedule.ShiftEndtDateTime);

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
        private async Task<Result> HandleRestDay(Command command, EmployeeResponse employee, CancellationToken cancellationToken)
        {
            var punches = await db.Punches
                   .Where(e => e.EmployeeBadge == command.EmployeeBadge &&
                           e.PunchOccurredAt.Date == command.PunchOccurredAt.Date)
                   .OrderBy(e => e.PunchOccurredAt)
                   .ToListAsync(cancellationToken);

            var attendanceRecords = await db.AttendanceRecords
                .Where(e => e.EmployeeId == employee.EmployeeId &&
                        e.CheckInAt.Date == command.PunchOccurredAt.Date)
                .OrderBy(e => e.CheckInAt)
                .ToListAsync(cancellationToken);
            foreach (var punch in punches)
            {
                var lastRecord = attendanceRecords
                   .OrderBy(e => e.CheckInAt)
                   .LastOrDefault();
                // here we assume that the expected check-in and check-out times are both at 8 AM on the punch date,
                // since it's a rest day and we don't have a work schedule to refer to.
                var expectedCheckInDate = punch.PunchOccurredAt.Date.AddHours(8);
                var expectedCheckOutDate = punch.PunchOccurredAt.Date.AddHours(8);
                if (lastRecord is null || lastRecord.CheckOutAt.HasValue)
                {
                    var newRecord = AttendanceRecord.Create(command.MachineId, employee.EmployeeId);
                    try
                    {
                        newRecord.RegisterCheckIn(punch.PunchOccurredAt,
                                                   expectedCheckInDate,
                                                   lastRecord);
                    }
                    catch (DomainException)
                    {
                        continue;
                    }
                    attendanceRecords.Add(newRecord);
                    continue;
                }
                // here we assume standared work is 0 hours since today is rest day 
                var schedule = new Modules.Attendence.Domain.AttendenceRecords.WorkSchedule(
                             new TimeSpan(0),
                            expectedCheckOutDate);

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
