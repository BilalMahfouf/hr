using System.Net.Http.Json;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.ZKTeco.Gateway.Requests;
using Modules.Attendence.Infrastructure.ZKTeco.Gateway.Responses;

namespace Modules.Attendence.Infrastructure.ZKTeco;

public sealed class ZKTecoGatwayMachineReader(
    HttpClient httpClient) : IAttendanceMachineReader
{
    public async Task<IReadOnlyList<RawAttendanceLog>> GetLogsAsync(
        AttendenceMachine machine,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var request = new ZKTecoGatewayRequest(
            from,
            to,
            machine.IpAddress,
            machine.Port,
            machine.MachineNumber);

        var response = await httpClient.PostAsJsonAsync(
            "api/zkteco/",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        if (response.Content is null)
        {
            return Array.Empty<RawAttendanceLog>();
        }

        var logs = await response.Content.ReadFromJsonAsync<
            List<ZKTecoGatewayLogResponse>>(cancellationToken);

        if (logs is null)
        {
            return Array.Empty<RawAttendanceLog>();
        }

        return logs
            .Select(r => new RawAttendanceLog(
                MachineId: machine.Id,
                EmployeeNumber: r.EmployeeNumber,
                Timestamp: r.Timestamp,
                VerifyMode: r.VerifyMode,
                InOutMode: r.InOutMode,
                WorkCode: r.WorkCode,
                DeviceSerialNumber: r.DeviceSerialNumber,
                MachineNumber: r.MachineNumber))
            .ToList();
    }
}
