using Modules.Attendence.Domain.Machines;

namespace Modules.Attendence.Application.Abstractions;

public interface IAttendanceMachineReader
{
    Task<IReadOnlyList<RawAttendanceLog>> GetLogsAsync(
        AttendenceMachine machine,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}

public sealed record RawAttendanceLog(
    MachineId MachineId,
    string EmployeeNumber,
    DateTime Timestamp,
    int VerifyMode,
    int InOutMode,
    int WorkCode,
    string? DeviceSerialNumber,
    int MachineNumber
);