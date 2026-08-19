namespace Modules.Shared.Util;

public static class AttendanceTime
{
    private static readonly TimeZoneInfo AlgeriaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Africa/Algiers");

    public static DateTime DeviceLocalToUtc(DateTime local)
        => TimeZoneInfo.ConvertTimeToUtc(local, AlgeriaTimeZone);
}
