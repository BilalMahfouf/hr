namespace Modules.Attendence.Infrastructure.ZKTeco;

public interface IZKemSession : IDisposable
{
    bool ConnectNet(string ipAddress, int port);

    int GetLastError();

    bool GetSerialNumber(int machineNumber, out string serialNumber);

    bool ReadGeneralLogData(int machineNumber);

    bool GetGeneralLogData(
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
}

public interface IZKemSessionFactory
{
    IZKemSession Create();
}