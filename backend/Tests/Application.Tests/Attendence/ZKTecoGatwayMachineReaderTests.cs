using System.Net;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.ZKTeco;
using Moq;
using Moq.Protected;

namespace Application.Tests.Attendence;

public sealed class ZKTecoGatwayMachineReaderTests
{
    private static AttendenceMachine Machine()
        => AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

    private static (ZKTecoGatwayMachineReader reader, Mock<HttpMessageHandler> handler) Arrange(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        object? responseContent = null)
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = responseContent is not null
                    ? JsonContent.Create(responseContent)
                    : null
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        return (new ZKTecoGatwayMachineReader(httpClient), handler);
    }

    [Fact]
    public async Task GetLogsAsync_SendsCorrectRequest()
    {
        var (reader, handler) = Arrange(
            responseContent: Array.Empty<object>());

        var machine = Machine();
        var from = new DateOnly(2026, 8, 17);
        var to = new DateOnly(2026, 8, 20);

        await reader.GetLogsAsync(machine, from, to);

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("api/zkteco/")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetLogsAsync_MapsResponseToRawAttendanceLog()
    {
        var gatewayLogs = new[]
        {
            new
            {
                EmployeeNumber = "100",
                Timestamp = new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc),
                VerifyMode = 1,
                InOutMode = 0,
                WorkCode = 0,
                DeviceSerialNumber = "SN-1",
                MachineNumber = 1
            }
        };

        var (reader, _) = Arrange(responseContent: gatewayLogs);
        var machine = Machine();

        var result = await reader.GetLogsAsync(
            machine,
            new DateOnly(2026, 8, 17),
            new DateOnly(2026, 8, 17));

        var single = Assert.Single(result);
        Assert.Equal(machine.Id, single.MachineId);
        Assert.Equal("100", single.EmployeeNumber);
        Assert.Equal(new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc), single.Timestamp);
        Assert.Equal(1, single.VerifyMode);
        Assert.Equal(0, single.InOutMode);
        Assert.Equal(0, single.WorkCode);
        Assert.Equal("SN-1", single.DeviceSerialNumber);
        Assert.Equal(1, single.MachineNumber);
    }

    [Fact]
    public async Task GetLogsAsync_MultipleLogs_AllMapped()
    {
        var gatewayLogs = new[]
        {
            new
            {
                EmployeeNumber = "100",
                Timestamp = new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc),
                VerifyMode = 1,
                InOutMode = 0,
                WorkCode = 0,
                DeviceSerialNumber = (string?)null,
                MachineNumber = 1
            },
            new
            {
                EmployeeNumber = "200",
                Timestamp = new DateTime(2026, 8, 17, 10, 30, 0, DateTimeKind.Utc),
                VerifyMode = 0,
                InOutMode = 1,
                WorkCode = 1,
                DeviceSerialNumber = "SN-2",
                MachineNumber = 1
            }
        };

        var (reader, _) = Arrange(responseContent: gatewayLogs);
        var machine = Machine();

        var result = await reader.GetLogsAsync(
            machine,
            new DateOnly(2026, 8, 17),
            new DateOnly(2026, 8, 17));

        Assert.Equal(2, result.Count);
        Assert.Equal("100", result[0].EmployeeNumber);
        Assert.Equal("200", result[1].EmployeeNumber);
        Assert.Equal(1, result[1].WorkCode);
    }

    [Fact]
    public async Task GetLogsAsync_EmptyJsonArray_ReturnsEmptyList()
    {
        var (reader, _) = Arrange(responseContent: new object[0]);
        var machine = Machine();

        var result = await reader.GetLogsAsync(
            machine,
            new DateOnly(2026, 8, 17),
            new DateOnly(2026, 8, 17));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLogsAsync_WhenNonSuccessStatus_ThrowsHttpRequestException()
    {
        var (reader, _) = Arrange(statusCode: HttpStatusCode.InternalServerError);
        var machine = Machine();

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            reader.GetLogsAsync(
                machine,
                new DateOnly(2026, 8, 17),
                new DateOnly(2026, 8, 17)));
    }

    [Fact]
    public async Task GetLogsAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage _, CancellationToken ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage();
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var reader = new ZKTecoGatwayMachineReader(httpClient);
        var machine = Machine();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.GetLogsAsync(
                machine,
                new DateOnly(2026, 8, 17),
                new DateOnly(2026, 8, 17),
                cts.Token));
    }
}
