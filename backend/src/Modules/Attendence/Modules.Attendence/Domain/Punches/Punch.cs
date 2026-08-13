using Modules.Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.Punches;

public sealed class Punch : Entity
{
    private Punch()
    {
    }

    private Punch(
        PunchId id,
        MachineId machineId,
        int employeeBadge,
        DateTime punchOccurredAt,
        DateTime createdOnUtc
        )
    {
        Id = id;
        MachineId = machineId;
        EmployeeBadge = employeeBadge;
        PunchOccurredAt = punchOccurredAt;
        CreatedOnUtc = createdOnUtc;
    }

    public PunchId Id { get; private set; }

    public MachineId MachineId { get; private set; }

    public int EmployeeBadge { get; private set; }

    /// <summary>
    /// Time reported by the biometric device.
    /// </summary>
    public DateTime PunchOccurredAt { get; private set; }

    public static Punch Create(
        MachineId machineId,
        int employeeBadge,
        DateTime punchOccurredAt,
        DateTime createdOnUtc)
    {
        if (employeeBadge <= 0)
            throw new ArgumentOutOfRangeException(nameof(employeeBadge));


        var punch =  new Punch(
            PunchId.New(),
            machineId,
            employeeBadge,
            punchOccurredAt,
            createdOnUtc
            );
        punch.RaiseDomainEvent(new PunchCreatedDomainEvent(
            punch.MachineId,
            employeeBadge,
            punch.PunchOccurredAt));
        return punch;
    }
}
