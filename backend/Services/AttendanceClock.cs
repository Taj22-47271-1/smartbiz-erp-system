namespace SmartBizERP.Api.Services;

public static class AttendanceClock
{
    public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
        }
    }

    public static DateTime LocalNow(string timeZoneId)
    {
        var zone = ResolveTimeZone(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
    }

    public static DateTime LocalToUtc(DateOnly date, TimeOnly time, string timeZoneId)
    {
        var zone = ResolveTimeZone(timeZoneId);
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }
}
