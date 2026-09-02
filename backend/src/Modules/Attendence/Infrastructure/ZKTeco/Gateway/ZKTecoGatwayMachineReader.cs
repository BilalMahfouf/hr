using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.ZKTeco.Gateway.Requests;
using Modules.Attendence.Infrastructure.ZKTeco.Gateway.Responses;

namespace Modules.Attendence.Infrastructure.ZKTeco.Gateway;

public sealed class ZKTecoGatwayMachineReader(
    HttpClient httpClient, IOptions<ZKTecoGatewayOptions> options) : IAttendanceMachineReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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
            "/api/zkteco/",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        if (response.Content is null)
        {
            return Array.Empty<RawAttendanceLog>();
        }

        var logs = await response.Content.ReadFromJsonAsync<
            List<ZKTecoGatewayLogResponse>>(JsonOptions, cancellationToken);

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


    public async Task ConnectAsync(string ip, int port, CancellationToken ct)
    {
        var request = new
        {
            IpAddress = ip,
            Port = port,
            MachineNumber = 6
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/api/zkteco/connect",
                request,
                JsonOptions,
                ct);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Console.WriteLine($"ZKTeco connect error: {e.Message}");
        }
    }

}
