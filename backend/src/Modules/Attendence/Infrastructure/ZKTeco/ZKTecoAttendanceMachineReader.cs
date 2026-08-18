using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Domain.Machines;

namespace Modules.Attendence.Infrastructure.ZKTeco;

public sealed class ZKTecoAttendanceMachineReader : IAttendanceMachineReader
{
    internal ZKTecoAttendanceMachineReader(IZKemSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    private readonly IZKemSessionFactory _sessionFactory;

    public  Task<IReadOnlyList<RawAttendanceLog>> GetLogsAsync(
        AttendenceMachine machine,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException(
                "'from' cannot be after 'to'.",
                nameof(from));
        }

        var logs = new List<RawAttendanceLog>();

        using var session = _sessionFactory.Create();

        Connect(session, machine);

        ReadGeneralLogs(session, machine);

        var serial = GetSerialNumber(session, machine);

        ReadLogs(
            session,
            machine,
            serial,
            from,
            to,
            logs,
            cancellationToken);

        return Task.FromResult<IReadOnlyList<RawAttendanceLog>>(logs);
    }

    private static void Connect(
        IZKemSession session,
        AttendenceMachine machine)
    {
        if (session.ConnectNet(machine.IpAddress, machine.Port))
        {
            return;
        }

        var error = session.GetLastError();

        throw new InvalidOperationException(
            $"Failed to connect to attendance machine " +
            $"'{machine.MachineNumber}' ({machine.IpAddress}:{machine.Port}). " +
            $"SDK error: {error}");
    }

    private static void ReadGeneralLogs(
        IZKemSession session,
        AttendenceMachine machine)
    {
        if (session.ReadGeneralLogData(machine.MachineNumber))
        {
            return;
        }

        var error = session.GetLastError();

        throw new InvalidOperationException(
            $"Failed to read logs from machine " +
            $"'{machine.MachineNumber}'. SDK error: {error}");
    }

    private static string? GetSerialNumber(
        IZKemSession session,
        AttendenceMachine machine)
    {
        if (session.GetSerialNumber(
                machine.MachineNumber,
                out string serial))
        {
            return serial;
        }

        return null;
    }

    private static void ReadLogs(
        IZKemSession session,
        AttendenceMachine machine,
        string? serial,
        DateOnly from,
        DateOnly to,
        List<RawAttendanceLog> logs,
        CancellationToken cancellationToken)
    {
        string enrollNumber;

        int verifyMode;
        int inOutMode;

        int year;
        int month;
        int day;
        int hour;
        int minute;
        int second;

        int workCode = 0;

        while (session.GetGeneralLogData(
            machine.MachineNumber,
            out enrollNumber,
            out verifyMode,
            out inOutMode,
            out year,
            out month,
            out day,
            out hour,
            out minute,
            out second,
            ref workCode))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var timestamp = new DateTime(
                year,
                month,
                day,
                hour,
                minute,
                second);

            var date = DateOnly.FromDateTime(timestamp);

            if (date < from || date > to)
                continue;

            logs.Add(new RawAttendanceLog(
                MachineId: machine.Id,
                EmployeeNumber: enrollNumber,
                Timestamp: timestamp,
                VerifyMode: verifyMode,
                InOutMode: inOutMode,
                WorkCode: workCode,
                DeviceSerialNumber: serial,
                MachineNumber: machine.MachineNumber));
        }
    }
}