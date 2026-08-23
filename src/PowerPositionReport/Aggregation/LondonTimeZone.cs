namespace PowerPositionReport.Aggregation;

/// <summary>
/// The Europe/London time zone, resolved once and shared.
///
/// It lives next to the aggregation code because "local time is Europe/London" is a rule of the
/// problem itself, not an infrastructure detail: it decides which local hour each period falls
/// into, and which calendar day the day-ahead extraction should ask for.
/// </summary>
public static class LondonTimeZone
{
    public static readonly TimeZoneInfo Instance = Resolve();

    private static TimeZoneInfo Resolve()
    {
        // "Europe/London" is the IANA identifier (works on Linux/macOS and on modern Windows
        // with ICU). On older Windows versions the same time zone is identified as
        // "GMT Standard Time". We try both so the code doesn't depend on the operating system.
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
    }
}
