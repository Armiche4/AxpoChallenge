using Axpo;

namespace PowerPositionReport.Aggregation;

/// <summary>
/// Period 1 starts at 23:00 local time.
///
/// Periods are grouped by that instant, NOT by the "HH:mm" label. On the day the clocks go back the
/// label 01:00 covers two different delivery hours; grouping by label would silently merge them into
/// one row. On the day the clocks go forward it would make the last period collide with period 1.
/// </summary>
public sealed class PowerPositionAggregator : IPowerPositionAggregator
{
    public IReadOnlyList<HourlyVolume> Aggregate(IEnumerable<PowerTrade> trades)
    {
        ArgumentNullException.ThrowIfNull(trades);

        var volumeByHourStart = new Dictionary<DateTimeOffset, double>();

        foreach (var trade in trades)
        {
            var dayStartUtc = GetLocalDayStartUtc(trade.Date);

            foreach (var period in trade.Periods)
            {
                var hourStart = TimeZoneInfo.ConvertTime(
                    new DateTimeOffset(dayStartUtc.AddHours(period.Period - 1)),
                    LondonTimeZone.Instance);

                volumeByHourStart[hourStart] = volumeByHourStart.GetValueOrDefault(hourStart) + period.Volume;
            }
        }

        // DateTimeOffset orders by absolute instant, so this is chronological day order even when
        // two rows carry the same "HH:mm" label.
        return volumeByHourStart
            .OrderBy(entry => entry.Key)
            .Select(entry => new HourlyVolume(TimeOnly.FromDateTime(entry.Key.DateTime), entry.Value))
            .ToList();
    }

    private static DateTime GetLocalDayStartUtc(DateTime tradeDate)
    {
     
        var previousDay = tradeDate.Date.AddDays(-1);
        var localDayStart = new DateTime(previousDay.Year, previousDay.Month, previousDay.Day, 23, 0, 0);

        return TimeZoneInfo.ConvertTimeToUtc(localDayStart, LondonTimeZone.Instance);
    }
}
