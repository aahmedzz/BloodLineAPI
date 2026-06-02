using System;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
    TimeZoneInfo LocalTimeZone { get; }
    DateTime ToLocalTime(DateTime utcDateTime);
    DateTime ToUtcTime(DateTime localDateTime);
    DateOnly CurrentLocalDate { get; }
    TimeSpan CurrentLocalTimeOfDay { get; }
}
