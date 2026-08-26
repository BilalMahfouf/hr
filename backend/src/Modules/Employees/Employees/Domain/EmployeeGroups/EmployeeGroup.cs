using Microsoft.AspNetCore.Mvc.Filters;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.Employees.Domain.EmployeeGroups;

public sealed class EmployeeGroup : Entity
{
    public new EmployeeGroupId Id { get; private set; }

    public string Name { get; private set; } = null!;

    public int NumberOfRotations => _rotationEntries.Count;

    public DateOnly RotationStartDate { get; private set; }

    public bool IsSecurity { get; private set; }

    public string? Description { get; private set; }

    private readonly List<WorkSchedule> _workSchedules = new();
    private readonly List<RotationEntry> _rotationEntries = new();

    public IReadOnlyCollection<WorkSchedule> WorkSchedules => _workSchedules.AsReadOnly();
    public IReadOnlyCollection<RotationEntry> RotationEntries => _rotationEntries.AsReadOnly();
    private EmployeeGroup()
    {
    }

    private EmployeeGroup(
        EmployeeGroupId id,
        string name,
        bool isSecurity,
        string? description)
    {
        Id = id;
        Name = name;
        IsSecurity = isSecurity;
        Description = description;
    }

    public static EmployeeGroup Create(
        string name,
        bool isSecurity,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(EmployeeGroupErrors.InvalidName);

        return new EmployeeGroup(
    EmployeeGroupId.New(),
    name,
    isSecurity,
    description);
    }

    public void AddWorkSchedule(CreateWorkScheduleDto schedule)
    {
        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }
        var newSchedule = WorkSchedule.Create(
            schedule.EmployeeGroupId,
            schedule.ShiftStartTime,
            schedule.ShiftEndTime,
            schedule.BreakStartTime,
            schedule.BreakEndTime,
            schedule.AllowedCheckInLatenessMinutes,
            schedule.AllowedCheckOutEarlinessMinutes,
            schedule.EndDayOffset);

        _workSchedules.Add(newSchedule);
    }
    public void UpdateWorkSchedule(UpdateWorkScheduleDto schedule)
    {
        var existingSchedule = _workSchedules.First(ws => ws.Id == schedule.Id);
        _workSchedules.Remove(existingSchedule);
        _workSchedules.Add(WorkSchedule.Create(
            schedule.EmployeeGroupId,
            schedule.ShiftStartTime,
            schedule.ShiftEndTime,
            schedule.BreakStartTime,
            schedule.BreakEndTime,
            schedule.AllowedCheckInLatenessMinutes,
            schedule.AllowedCheckOutEarlinessMinutes,
            schedule.EndDayOffset));
    }
    public void RemoveWorkSchedule(WorkSchedule schedule)
    {
        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }
        _workSchedules.Remove(schedule);
    }
    public void ActivateWorkSchedule(WorkSchedule schedule)
    {
        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }
        schedule.Activate();
    }
    public void DeactivateWorkSchedule(WorkSchedule schedule)
    {
        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }
        schedule.Deactivate();
    }

    public bool DoesTheGroupWork(DateOnly date)
    {
        var rotation = GetRotation(date);
        return rotation?.Status is RotationStatus.Work ? true : false;
    }
    private RotationEntry? GetRotation(DateOnly date)
    {
        if (date < RotationStartDate)
        {
            throw new DomainException();
        }
        var daysElapsed = date.DayNumber - RotationStartDate.DayNumber;
        var position = (daysElapsed % NumberOfRotations) + 1;
        var rotation = _rotationEntries.FirstOrDefault(e => e.Position == position);
        return rotation is not null ? rotation : null;
    }
    public WorkScheduleResponse? GetGroupWorkScheduleInDateTime(DateOnly date)
    {
        var rotation = GetRotation(date);
        if (rotation is null)
        {
            return null;
        }
        if (rotation.Status == RotationStatus.Rest)
        {
            var prevRotation = _rotationEntries.FirstOrDefault(e => e.Position == rotation.Position - 1);
            if (prevRotation != null &&
                prevRotation.WorkSchedule != null &&
                prevRotation.WorkSchedule.EndDayOffset > 0)
            {
                var expectedCheckin = date
                     .AddDays(-prevRotation.WorkSchedule.EndDayOffset)
                     .ToDateTime(prevRotation.WorkSchedule.ShiftStartTime);
                var expectedCheckout = date
                    .ToDateTime(prevRotation.WorkSchedule.ShiftEndTime);

                return new WorkScheduleResponse(expectedCheckin, expectedCheckout);
            }
            return null;

        }
        var schedule = rotation.WorkSchedule;
        var expectedCheckIn = date.ToDateTime(schedule!.ShiftStartTime);
        var expectedCheckOut = date.AddDays(schedule.EndDayOffset).ToDateTime(schedule.ShiftEndTime);
        return new WorkScheduleResponse(expectedCheckIn, expectedCheckOut);

    }
}
public sealed record WorkScheduleResponse(DateTime ExpectedCheckInAt, DateTime ExpectedCheckoutAt);