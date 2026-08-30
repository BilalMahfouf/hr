using Modules.Shared.Domain.Common;

namespace Modules.Attendence.Domain.Machines;

public sealed class AttendenceMachine : Entity
{
    public new MachineId Id { get; private set; }

    public int MachineNumber { get; private set; }
    public string IpAddress { get; private set; } = null!;
    public int Port { get; private set; }
    public MachineType Type { get; private set; }

    public bool IsActive { get; private set; }

    private AttendenceMachine()
    {
    }

    public static AttendenceMachine Create(
        MachineId id,
        string ipAddress,
        int machineNumber,
        MachineType type,
        int? port = null)
    {
        var machine = new AttendenceMachine
        {
            Id = id,
            IpAddress = ipAddress,
            MachineNumber = machineNumber,
            Type = type,
            Port = port ?? 4370,
            IsActive = true
        };
        return machine;
    }

    public void Update(string ipAddress, int port)
    {
        IpAddress = ipAddress;
        Port = port;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}