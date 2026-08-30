namespace Modules.Attendence.Infrastructure.ZKTeco.Gateway.Responses;

internal sealed record ZKTecoGatewayLogResponse(
    string EmployeeNumber,
    DateTime Timestamp,
    int VerifyMode,
    int InOutMode,
    int WorkCode,
    string? DeviceSerialNumber,
    int MachineNumber);
