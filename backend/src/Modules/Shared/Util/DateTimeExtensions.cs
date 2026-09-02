using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Shared.Util;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime dateTime)
        => dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(
                dateTime,
                DateTimeKind.Utc),

            _ => throw new ArgumentOutOfRangeException()
        };

    public static DateTime ToUtcStartOfDay(this DateOnly date)
        => DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

    public static DateTime ToUtcExclusiveEndOfDay(this DateOnly date)
        => DateTime.SpecifyKind(
            date.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
}