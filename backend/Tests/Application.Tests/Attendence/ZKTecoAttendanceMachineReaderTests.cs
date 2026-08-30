using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.ZKTeco;
using Moq;

namespace Application.Tests.Attendence;

public sealed class ZKTecoAttendanceMachineReaderTests
{
    private static readonly DateOnly From = new(2026, 8, 17);
    private static readonly DateOnly To = new(2026, 8, 17);

    private static AttendenceMachine Machine()
        => AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoSdk);

    private sealed record Log(
        string Enroll,
        DateTime Time,
        int Verify,
        int InOut,
        int Work);

    private delegate void SetSerialCallback(
        int machineNumber,
        out string serialNumber);

    private delegate bool GetGeneralLogDataCallback(
        int machineNumber,
        out string enrollNumber,
        out int verifyMode,
        out int inOutMode,
        out int year,
        out int month,
        out int day,
        out int hour,
        out int minute,
        out int second,
        ref int workCode);

    private static (
        Mock<IZKemSessionFactory> factory,
        Mock<IZKemSession> session,
        AttendenceMachine machine) Arrange(
            Queue<Log> logs,
            string serial = "SN-1",
            bool connectOk = true,
            bool readOk = true,
            int lastError = 0)
    {
        var machine = Machine();

        var session = new Mock<IZKemSession>();
        var factory = new Mock<IZKemSessionFactory>();
        factory.Setup(f => f.Create()).Returns(session.Object);

        session
            .Setup(s => s.ConnectNet(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(connectOk);
        session
            .Setup(s => s.GetLastError())
            .Returns(lastError);
        session
            .Setup(s => s.ReadGeneralLogData(It.IsAny<int>()))
            .Returns(readOk);
        session
            .Setup(s => s.GetSerialNumber(It.IsAny<int>(), out It.Ref<string>.IsAny))
            .Callback(new SetSerialCallback((int machineNumber, out string serialNumber) =>
                serialNumber = serial))
            .Returns(true);
        session
            .Setup(s => s.GetGeneralLogData(
                It.IsAny<int>(),
                out It.Ref<string>.IsAny,
                out It.Ref<int>.IsAny,
                out It.Ref<int>.IsAny,
                out It.Ref<int>.IsAny,
                out It.Ref<int>.IsAny,
                out It.Ref<int>.IsAny,
                out It.Ref<int>.IsAny,
                out It.Ref<int>.IsAny,
                out It.Ref<int>.IsAny,
                ref It.Ref<int>.IsAny))
            .Returns(new GetGeneralLogDataCallback((int machineNumber,
                out string enrollNumber,
                out int verifyMode,
                out int inOutMode,
                out int year,
                out int month,
                out int day,
                out int hour,
                out int minute,
                out int second,
                ref int workCode) =>
            {
                if (logs.Count == 0)
                {
                    enrollNumber = string.Empty;
                    verifyMode = 0;
                    inOutMode = 0;
                    year = 0;
                    month = 0;
                    day = 0;
                    hour = 0;
                    minute = 0;
                    second = 0;
                    workCode = 0;
                    return false;
                }

                var log = logs.Dequeue();
                enrollNumber = log.Enroll;
                verifyMode = log.Verify;
                inOutMode = log.InOut;
                year = log.Time.Year;
                month = log.Time.Month;
                day = log.Time.Day;
                hour = log.Time.Hour;
                minute = log.Time.Minute;
                second = log.Time.Second;
                workCode = log.Work;
                return true;
            }));

        return (factory, session, machine);
    }

    private static ZKTecoAttendanceMachineReader CreateReader(
        Mock<IZKemSessionFactory> factory)
        => new(factory.Object);

    [Fact]
    public async Task GetLogsAsync_WhenFromAfterTo_Throws()
    {
        var (factory, _, machine) = Arrange(new Queue<Log>());

        var reader = CreateReader(factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            reader.GetLogsAsync(machine, new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 16)));
        factory.Verify(f => f.Create(), Times.Never);
    }

    [Fact]
    public async Task GetLogsAsync_WhenConnectionFails_ThrowsWithSdkErrorAndDisconnects()
    {
        var (factory, session, machine) = Arrange(
            new Queue<Log>(),
            connectOk: false,
            lastError: 501);

        var reader = CreateReader(factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.GetLogsAsync(machine, From, To));

        Assert.Contains("501", exception.Message);
        session.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_WhenReadGeneralLogsFails_ThrowsWithSdkErrorAndDisconnects()
    {
        var (factory, session, machine) = Arrange(
            new Queue<Log>(),
            readOk: false,
            lastError: 502);

        var reader = CreateReader(factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.GetLogsAsync(machine, From, To));

        Assert.Contains("502", exception.Message);
        session.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_MapsSdkLogToRawAttendanceLog()
    {
        var logs = new Queue<Log>();
        logs.Enqueue(new Log(
            "100",
            new DateTime(2026, 8, 17, 9, 30, 0),
            Verify: 1,
            InOut: 0,
            Work: 0));

        var (factory, session, machine) = Arrange(logs, serial: "SN-1");

        var reader = CreateReader(factory);

        var result = await reader.GetLogsAsync(machine, From, To);

        var single = Assert.Single(result);
        Assert.Equal(machine.Id, single.MachineId);
        Assert.Equal(1, single.MachineNumber);
        Assert.Equal("100", single.EmployeeNumber);
        Assert.Equal(new DateTime(2026, 8, 17, 8, 30, 0, DateTimeKind.Utc), single.Timestamp);
        Assert.Equal(1, single.VerifyMode);
        Assert.Equal(0, single.InOutMode);
        Assert.Equal(0, single.WorkCode);
        Assert.Equal("SN-1", single.DeviceSerialNumber);
        session.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_ConvertsDeviceLocalTimeToUtc()
    {
        var logs = new Queue<Log>();
        logs.Enqueue(new Log(
            "100",
            new DateTime(2026, 8, 17, 9, 0, 0),
            Verify: 1,
            InOut: 0,
            Work: 0));

        var (factory, session, machine) = Arrange(logs, serial: "SN-1");

        var reader = CreateReader(factory);

        var result = await reader.GetLogsAsync(machine, From, To);

        var single = Assert.Single(result);
        Assert.Equal(
            new DateTime(2026, 8, 17, 8, 0, 0, DateTimeKind.Utc),
            single.Timestamp);
        Assert.Equal(DateTimeKind.Utc, single.Timestamp.Kind);
        session.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_SingleDay_KeepsOnlyThatDay()
    {
        var logs = new Queue<Log>();
        logs.Enqueue(new Log("1", new DateTime(2026, 8, 17, 9, 0, 0), 1, 0, 0));
        logs.Enqueue(new Log("2", new DateTime(2026, 8, 18, 9, 0, 0), 1, 0, 0));
        logs.Enqueue(new Log("3", new DateTime(2026, 8, 16, 9, 0, 0), 1, 0, 0));

        var (factory, _, machine) = Arrange(logs);

        var reader = CreateReader(factory);

        var result = await reader.GetLogsAsync(machine, From, To);

        var single = Assert.Single(result);
        Assert.Equal("1", single.EmployeeNumber);
    }

    [Fact]
    public async Task GetLogsAsync_DateRange_IsInclusive()
    {
        var logs = new Queue<Log>();
        logs.Enqueue(new Log("boundary-from", new DateTime(2026, 8, 1, 1, 0, 0), 1, 0, 0));
        logs.Enqueue(new Log("boundary-to", new DateTime(2026, 8, 3, 23, 59, 59), 1, 0, 0));
        logs.Enqueue(new Log("outside", new DateTime(2026, 8, 4, 9, 0, 0), 1, 0, 0));

        var (factory, _, machine) = Arrange(logs);

        var reader = CreateReader(factory);

        var result = await reader.GetLogsAsync(
            machine,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 3));

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.EmployeeNumber == "boundary-from");
        Assert.Contains(result, x => x.EmployeeNumber == "boundary-to");
    }

    [Fact]
    public async Task GetLogsAsync_WhenCancelled_ThrowsAndDisconnects()
    {
        var logs = new Queue<Log>();
        logs.Enqueue(new Log("1", new DateTime(2026, 8, 17, 9, 0, 0), 1, 0, 0));

        var (factory, session, machine) = Arrange(logs);

        var reader = CreateReader(factory);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            reader.GetLogsAsync(machine, From, To, cts.Token));
        session.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_DisconnectsOnSuccess()
    {
        var logs = new Queue<Log>();
        logs.Enqueue(new Log("1", new DateTime(2026, 8, 17, 9, 0, 0), 1, 0, 0));

        var (factory, session, machine) = Arrange(logs);

        var reader = CreateReader(factory);

        await reader.GetLogsAsync(machine, From, To);

        session.Verify(s => s.Dispose(), Times.Once);
    }
}