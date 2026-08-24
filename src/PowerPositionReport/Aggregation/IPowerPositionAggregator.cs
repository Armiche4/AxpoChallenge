using Axpo;

namespace PowerPositionReport.Aggregation;

/// <summary>
/// Aggregates the periods of a collection of <see cref="PowerTrade"/> into total volumes per local hour.
/// it is only responsible for the calculation.
/// </summary>
public interface IPowerPositionAggregator
{
    /// <summary>
    /// Sums the volume of every period of every trade, grouped by local hour (Europe/London),
    /// returning one row per hour in day order (starting at 23:00 the previous day.
    /// </summary>
    IReadOnlyList<HourlyVolume> Aggregate(IEnumerable<PowerTrade> trades);
}
