using System;
using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BloodLineAPI.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _localTimeZone;

    public DateTimeProvider(IConfiguration configuration)
    {
        var timeZoneId = configuration["TimeZoneId"] ?? "Egypt Standard Time";
        try
        {
            _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback to UTC if timezone is not found
            _localTimeZone = TimeZoneInfo.Utc;
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _localTimeZone);

    public TimeZoneInfo LocalTimeZone => _localTimeZone;

    public DateTime ToLocalTime(DateTime utcDateTime)
    {
        // Make sure the input DateTime is treated as UTC
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            
        return TimeZoneInfo.ConvertTimeFromUtc(utc, _localTimeZone);
    }

    public DateTime ToUtcTime(DateTime localDateTime)
    {
        // Make sure the input DateTime is treated as local timezone
        var local = localDateTime.Kind == DateTimeKind.Unspecified || localDateTime.Kind == DateTimeKind.Local
            ? DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified)
            : localDateTime;

        return TimeZoneInfo.ConvertTime(local, _localTimeZone, TimeZoneInfo.Utc);
    }

    public DateOnly CurrentLocalDate => DateOnly.FromDateTime(LocalNow);

    public TimeSpan CurrentLocalTimeOfDay => LocalNow.TimeOfDay;
}
