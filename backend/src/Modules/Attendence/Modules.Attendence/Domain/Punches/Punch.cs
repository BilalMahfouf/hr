using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.Punches;

public sealed class Punch
{
    private Punch()
    {
    }

    private Punch(
        PunchId id,
        MachineId machineId,
        int employeeBadge,
        DateTime punchOccurredAt,
        DateTime createdOnUtc,
        Guid? createdByUserId)
    {
        Id = id;
        MachineId = machineId;
        EmployeeBadge = employeeBadge;
        PunchOccurredAt = punchOccurredAt;
        CreatedOnUtc = createdOnUtc;
        CreatedByUserId = createdByUserId;
    }

    public PunchId Id { get; private set; }

    public MachineId MachineId { get; private set; }

    public int EmployeeBadge { get; private set; }

    /// <summary>
    /// Time reported by the biometric device.
    /// </summary>
    public DateTime PunchOccurredAt { get; private set; }

    /// <summary>
    /// Time the punch was persisted in the system.
    /// </summary>
    public DateTime CreatedOnUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public static Punch Create(
        MachineId machineId,
        int employeeBadge,
        DateTime punchOccurredAt,
        DateTime createdOnUtc,
        Guid? createdByUserId = null)
    {
        if (employeeBadge <= 0)
            throw new ArgumentOutOfRangeException(nameof(employeeBadge));

        return new Punch(
            PunchId.New(),
            machineId,
            employeeBadge,
            punchOccurredAt,
            createdOnUtc,
            createdByUserId);
    }
}
