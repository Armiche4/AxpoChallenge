using Axpo;

namespace PowerPositionReport.Aggregation;

/// <summary>
/// Aggregates the periods of a collection of <see cref="PowerTrade"/> into total volumes per local hour.
/// On purpose, this interface knows nothing about files, CSV, or how the trades were obtained:
/// it is only responsible for the calculation (single responsibility principle). Placing it behind
/// an interface allows the implementation to be swapped (or replaced with a test double) without
/// touching whoever consumes it (dependency inversion principle).
/// </summary>
public interface IPowerPositionAggregator
{
    /// <summary>
    /// Sums the volume of every period of every trade, grouped by local hour (Europe/London),
    /// returning one row per hour in day order (starting at 23:00 the previous day, as required
    /// by the challenge).
    /// </summary>
    IReadOnlyList<HourlyVolume> Aggregate(IEnumerable<PowerTrade> trades);
}
