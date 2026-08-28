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
        string? description,
        DateOnly rotationStartDate)
    {
        Id = id;
        Name = name;
        IsSecurity = isSecurity;
        Description = description;
        RotationStartDate = rotationStartDate;
    }

    public static EmployeeGroup Create(
        string name,
        bool isSecurity,
        DateOnly rotationStartDate,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(EmployeeGroupErrors.InvalidName);

        if (rotationStartDate == default)
            throw new DomainException(EmployeeGroupErrors.RotationStartDateRequired);

        return new EmployeeGroup(
            EmployeeGroupId.New(),
            name,
            isSecurity,
            description,
            rotationStartDate);
    }

    public void SetRotationStartDate(DateOnly rotationStartDate)
    {
        if (rotationStartDate == default)
            throw new DomainException(EmployeeGroupErrors.RotationStartDateRequired);

        RotationStartDate = rotationStartDate;
    }

    public void UpdateDetails(string? name, bool? isSecurity, string? description)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException(EmployeeGroupErrors.InvalidName);
            Name = name;
        }

        if (isSecurity.HasValue)
            IsSecurity = isSecurity.Value;

        if (description is not null)
            Description = description;
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

    public WorkSchedule UpdateWorkSchedule(UpdateWorkScheduleDto schedule)
    {
        var existingSchedule = _workSchedules.FirstOrDefault(ws => ws.Id == schedule.Id);
        if (existingSchedule is null)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
        }

        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }

        var isReferenced = _rotationEntries.Any(re => re.WorkScheduleId == schedule.Id);
        if (isReferenced)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleInUse);
        }

        _workSchedules.Remove(existingSchedule);
        var updated = WorkSchedule.Create(
            schedule.EmployeeGroupId,
            schedule.ShiftStartTime,
            schedule.ShiftEndTime,
            schedule.BreakStartTime,
            schedule.BreakEndTime,
            schedule.AllowedCheckInLatenessMinutes,
            schedule.AllowedCheckOutEarlinessMinutes,
            schedule.EndDayOffset);
        _workSchedules.Add(updated);
        return updated;
    }

    public void RemoveWorkSchedule(WorkScheduleId scheduleId)
    {
        var schedule = _workSchedules.FirstOrDefault(ws => ws.Id == scheduleId);
        if (schedule is null)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
        }

        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }

        var isReferenced = _rotationEntries.Any(re => re.WorkScheduleId == scheduleId);
        if (isReferenced)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleInUse);
        }

        _workSchedules.Remove(schedule);
    }

    public void ActivateWorkSchedule(WorkScheduleId scheduleId)
    {
        var schedule = _workSchedules.FirstOrDefault(ws => ws.Id == scheduleId);
        if (schedule is null)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
        }

        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }

        schedule.Activate();
    }

    public void DeactivateWorkSchedule(WorkScheduleId scheduleId)
    {
        var schedule = _workSchedules.FirstOrDefault(ws => ws.Id == scheduleId);
        if (schedule is null)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
        }

        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }

        schedule.Deactivate();
    }

    public void AddRotationEntry(int position, WorkScheduleId? workScheduleId)
    {
        if (position < 1)
            throw new DomainException(RotationEntryErrors.InvalidPosition);

        if (_rotationEntries.Any(re => re.Position == position))
            throw new DomainException(EmployeeGroupErrors.DuplicateRotationPosition);

        if (workScheduleId.HasValue)
        {
            var schedule = _workSchedules.FirstOrDefault(ws => ws.Id == workScheduleId.Value);
            if (schedule is null)
                throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
        }

        var entry = RotationEntry.Create(Id, position, workScheduleId);
        _rotationEntries.Add(entry);
    }

    public void RemoveRotationEntry(int position)
    {
        var entry = _rotationEntries.FirstOrDefault(re => re.Position == position);
        if (entry is null)
            throw new DomainException(EmployeeGroupErrors.RotationEntryNotFound);

        _rotationEntries.Remove(entry);
    }

    public RotationEntry ReplaceRotationEntry(int position, int newPosition, WorkScheduleId? workScheduleId)
    {
        if (newPosition < 1)
            throw new DomainException(RotationEntryErrors.InvalidPosition);

        var entry = _rotationEntries.FirstOrDefault(re => re.Position == position);
        if (entry is null)
            throw new DomainException(EmployeeGroupErrors.RotationEntryNotFound);

        if (newPosition != position && _rotationEntries.Any(re => re.Position == newPosition))
            throw new DomainException(EmployeeGroupErrors.DuplicateRotationPosition);

        if (workScheduleId.HasValue)
        {
            var schedule = _workSchedules.FirstOrDefault(ws => ws.Id == workScheduleId.Value);
            if (schedule is null)
                throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
        }

        _rotationEntries.Remove(entry);
        var newEntry = RotationEntry.Create(Id, newPosition, workScheduleId);
        _rotationEntries.Add(newEntry);
        return newEntry;
    }

    public void ReplaceRotationEntries(IReadOnlyList<(int Position, WorkScheduleId? WorkScheduleId)> entries)
    {
        if (entries.Count == 0)
            throw new DomainException(EmployeeGroupErrors.InvalidRotationCount);

        var positions = entries.Select(e => e.Position).ToList();
        if (positions.Distinct().Count() != positions.Count)
            throw new DomainException(EmployeeGroupErrors.DuplicateRotationPosition);

        foreach (var (_, workScheduleId) in entries)
        {
            if (workScheduleId.HasValue)
            {
                var schedule = _workSchedules.FirstOrDefault(ws => ws.Id == workScheduleId.Value);
                if (schedule is null)
                    throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
            }
        }

        _rotationEntries.Clear();
        foreach (var (position, workScheduleId) in entries)
        {
            var entry = RotationEntry.Create(Id, position, workScheduleId);
            _rotationEntries.Add(entry);
        }
    }

    public void ReplaceSchedulesAndRotations(
        IReadOnlyList<CreateWorkScheduleDto> schedules,
        IReadOnlyList<(int Position, int? WorkScheduleIndex)> rotationEntries)
    {
        if (rotationEntries.Count == 0)
            throw new DomainException(EmployeeGroupErrors.InvalidRotationCount);

        var positions = rotationEntries.Select(e => e.Position).ToList();
        if (positions.Distinct().Count() != positions.Count)
            throw new DomainException(EmployeeGroupErrors.DuplicateRotationPosition);

        // Validate schedule indices before mutating any state.
        foreach (var (_, scheduleIndex) in rotationEntries)
        {
            if (scheduleIndex.HasValue && (scheduleIndex.Value < 0 || scheduleIndex.Value >= schedules.Count))
                throw new DomainException(EmployeeGroupErrors.WorkScheduleNotFound);
        }

        _rotationEntries.Clear();
        _workSchedules.Clear();

        foreach (var schedule in schedules)
        {
            AddWorkSchedule(schedule);
        }

        var createdSchedules = _workSchedules.ToList();
        foreach (var (position, scheduleIndex) in rotationEntries)
        {
            var workScheduleId = scheduleIndex.HasValue
                ? createdSchedules[scheduleIndex.Value].Id
                : (WorkScheduleId?)null;
            _rotationEntries.Add(RotationEntry.Create(Id, position, workScheduleId));
        }
    }

    public bool DoesTheGroupWork(DateOnly date)
    {
        var rotation = GetRotation(date);
        return rotation?.Status is RotationStatus.Work ? true : false;
    }

    public RotationEntry? GetRotation(DateOnly date)
    {
        if (date < RotationStartDate)
        {
            return null;
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