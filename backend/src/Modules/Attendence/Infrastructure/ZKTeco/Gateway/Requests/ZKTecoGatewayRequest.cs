namespace Modules.Attendence.Infrastructure.ZKTeco.Gateway.Requests;

internal sealed record ZKTecoGatewayRequest(
    DateOnly From,
    DateOnly To,
    string IpAddress,
    int Port,
    int MachineNumber);
