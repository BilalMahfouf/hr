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

    public void AddWorkSchedule(WorkSchedule schedule)
    {
        if (schedule.EmployeeGroupId != Id)
        {
            throw new DomainException(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup);
        }

        _workSchedules.Add(schedule);
    }
}