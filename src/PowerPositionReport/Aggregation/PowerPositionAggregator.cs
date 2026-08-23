using Axpo;

namespace PowerPositionReport.Aggregation;

/// <summary>
/// Implementation of <see cref="IPowerPositionAggregator"/>.
///
/// Key idea: every period lasts exactly 1 real hour (absolute/UTC time), and period 1 always
/// starts at 23:00 local time the previous day. So, to know which LOCAL hour each period
/// corresponds to, we first calculate its real UTC instant (day start + N hours) and then
/// convert it to Europe/London local time.
///
/// This resolves daylight saving time (DST) with no special-casing:
///   - Day the clocks go forward (spring): one local hour "disappears", so no row is produced
///     for that hour.
///   - Day the clocks go back (autumn): one local hour repeats, so the two periods that fall
///     on that same local hour are automatically summed into a single row.
/// </summary>
public sealed class PowerPositionAggregator : IPowerPositionAggregator
{
    public IReadOnlyList<HourlyVolume> Aggregate(IEnumerable<PowerTrade> trades)
    {
        ArgumentNullException.ThrowIfNull(trades);

        var volumeByLocalHour = new Dictionary<TimeOnly, double>();
        var orderedHours = new List<TimeOnly>();

        foreach (var trade in trades)
        {
            var dayStartUtc = GetLocalDayStartUtc(trade.Date);

            foreach (var period in trade.Periods)
            {
                var periodStartUtc = dayStartUtc.AddHours(period.Period - 1);
                var localHour = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(periodStartUtc, LondonTimeZone.Instance));

                if (volumeByLocalHour.TryGetValue(localHour, out var currentVolume))
                {
                    volumeByLocalHour[localHour] = currentVolume + period.Volume;
                }
                else
                {
                    volumeByLocalHour[localHour] = period.Volume;
                    orderedHours.Add(localHour);
                }
            }
        }

        return orderedHours
            .Select(hour => new HourlyVolume(hour, volumeByLocalHour[hour]))
            .ToList();
    }

    private static DateTime GetLocalDayStartUtc(DateTime tradeDate)
    {
        // The "day" tradeDate starts, in local time, at 23:00 the previous calendar day
        // (explicit requirement of the challenge). 23:00 never falls within the UK's
        // ambiguous/non-existent clock-change hour (which happens in the early morning),
        // so this conversion is always unambiguous.
        var previousDay = tradeDate.Date.AddDays(-1);
        var localDayStart = new DateTime(previousDay.Year, previousDay.Month, previousDay.Day, 23, 0, 0);

        return TimeZoneInfo.ConvertTimeToUtc(localDayStart, LondonTimeZone.Instance);
    }
}
