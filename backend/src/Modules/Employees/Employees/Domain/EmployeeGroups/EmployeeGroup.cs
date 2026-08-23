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

    public byte NumberOfRotations { get; private set; }

    public bool IsSecurity { get; private set; }

    public string? Description { get; private set; }

    private readonly List<WorkSchedule> _workSchedules = new();

    public IReadOnlyCollection<WorkSchedule> WorkSchedules => _workSchedules.AsReadOnly();

    private EmployeeGroup()
    {
    }

    private EmployeeGroup(
        EmployeeGroupId id,
        string name,
        byte numberOfRotations,
        bool isSecurity,
        string? description)
    {
        Id = id;
        Name = name;
        NumberOfRotations = numberOfRotations;
        IsSecurity = isSecurity;
        Description = description;
    }

    public static EmployeeGroup Create(
        string name,
        byte numberOfRotations,
        bool isSecurity,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(EmployeeGroupErrors.InvalidName);

        if (numberOfRotations == 0)
            throw new DomainException(EmployeeGroupErrors.InvalidNumberOfRotations);

        return new EmployeeGroup(
            EmployeeGroupId.New(),
            name,
            numberOfRotations,
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
}