using zkemkeeper;

namespace Modules.Attendence.Infrastructure.ZKTeco;

public sealed class ZkemSessionFactory : IZKemSessionFactory
{
    public IZKemSession Create() => new ZkemSession();
}

public sealed class ZkemSession : IZKemSession
{
    private readonly CZKEM _zk = new();

    public bool ConnectNet(string ipAddress, int port)
        => _zk.Connect_Net(ipAddress, port);

    public int GetLastError()
    {
        var error = 0;

        _zk.GetLastError(ref error);

        return error;
    }

    public bool GetSerialNumber(int machineNumber, out string serialNumber)
        => _zk.GetSerialNumber(machineNumber, out serialNumber);

    public bool ReadGeneralLogData(int machineNumber)
        => _zk.ReadGeneralLogData(machineNumber);

    public bool GetGeneralLogData(
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
        ref int workCode)
        => _zk.SSR_GetGeneralLogData(
            machineNumber,
            out enrollNumber,
            out verifyMode,
            out inOutMode,
            out year,
            out month,
            out day,
            out hour,
            out minute,
            out second,
            ref workCode);

    public void Dispose()
    {
        try
        {
            _zk.Disconnect();
        }
        catch
        {
            // Don't hide the original exception.
        }
    }
}