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
    public void AddWorkSchedule(
    TimeOnly shiftStartTime,
    TimeOnly shiftEndTime,
    TimeOnly breakStartTime,
    TimeOnly breakEndTime,
    int allowedCheckInLatenessMinutes,
    int allowedCheckOutEarlinessMinutes,
    int endDayOffset)
    {
        var workSchedule = WorkSchedule.Create(
            Id,
            shiftStartTime,
            shiftEndTime,
            breakStartTime,
            breakEndTime,
            allowedCheckInLatenessMinutes,
            allowedCheckOutEarlinessMinutes,
            endDayOffset);

        _workSchedules.Add(workSchedule);
    }

    public void UpdateWorkSchedule(
        WorkScheduleId workScheduleId,
        TimeOnly shiftStartTime,
        TimeOnly shiftEndTime,
        TimeOnly breakStartTime,
        TimeOnly breakEndTime,
        int allowedCheckInLatenessMinutes,
        int allowedCheckOutEarlinessMinutes,
        int endDayOffset)
    {
        var workSchedule = FindWorkSchedule(workScheduleId);

        workSchedule.Update(
            shiftStartTime,
            shiftEndTime,
            breakStartTime,
            breakEndTime,
            allowedCheckInLatenessMinutes,
            allowedCheckOutEarlinessMinutes,
            endDayOffset);
    }

    public void RemoveWorkSchedule(WorkScheduleId workScheduleId)
    {
        var workSchedule = FindWorkSchedule(workScheduleId);

        if (_rotationEntries.Any(x => x.WorkScheduleId == workScheduleId))
        {
            throw new DomainException(
                EmployeeGroupErrors.WorkScheduleUsedByRotation);
        }

        _workSchedules.Remove(workSchedule);
    }

    private WorkSchedule FindWorkSchedule(WorkScheduleId workScheduleId)
    {
        return _workSchedules.SingleOrDefault(x => x.Id == workScheduleId)
            ?? throw new DomainException(
                EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
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
    public void AddWorkRotation(
           int position,
           WorkScheduleId workScheduleId)
    {
        AddRotation(position, workScheduleId);
    }

    public void AddRestRotation(int position)
    {
        AddRotation(position, null);
    }

    public void UpdateRotation(
        int position,
        WorkScheduleId? workScheduleId)
    {
        var existingRotation = FindRotation(position);

        _rotationEntries.Remove(existingRotation);

        var replacement = RotationEntry.Create(
            Id,
            position,
            workScheduleId);

        _rotationEntries.Add(replacement);
    }

    public void RemoveRotation(int position)
    {
        var rotation = FindRotation(position);

        _rotationEntries.Remove(rotation);
    }

    private void AddRotation(
        int position,
        WorkScheduleId? workScheduleId)
    {
        EnsurePositionAvailable(position);

        var rotation = RotationEntry.Create(
            Id,
            position,
            workScheduleId);

        _rotationEntries.Add(rotation);
    }

    private RotationEntry FindRotation(int position)
    {
        return _rotationEntries.SingleOrDefault(x => x.Position == position)
            ?? throw new DomainException(
                EmployeeGroupErrors.RotationNotFound);
    }

    private void EnsurePositionAvailable(int position)
    {
        if (position < 1)
            throw new DomainException(
                RotationEntryErrors.InvalidPosition);

        if (_rotationEntries.Any(x => x.Position == position))
            throw new DomainException(
                EmployeeGroupErrors.RotationPositionAlreadyExists);
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