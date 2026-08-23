namespace PowerPositionReport.Extraction;

/// <summary>
/// Runs one full extraction: gets the day-ahead trades from the trading system, aggregates them
/// into hourly volumes, and writes the CSV report. "Extraction" is the challenge's own word for
/// this unit of work ("an extract must run at a scheduled time interval").
///
/// On purpose this interface knows nothing about scheduling or timers (that is the Worker's
/// job): it only knows how to run a single extraction from start to finish (single
/// responsibility principle). Placing it behind an interface lets the Worker depend on "run one
/// extraction" without knowing how it is implemented (dependency inversion principle), and lets
/// it be replaced with a fake in the Worker's tests.
/// </summary>
public interface IPowerPositionReportService
{
    /// <summary>
    /// Fetches tomorrow's day-ahead power trades, aggregates their volume per local hour,
    /// and writes the resulting CSV report to the configured output folder.
    /// Throws if any step fails (e.g. <c>Axpo.PowerServiceException</c> from the trading
    /// system, or an I/O error writing the file) — the caller decides how to handle that.
    /// </summary>
    Task RunExtractionAsync(CancellationToken cancellationToken);
}
