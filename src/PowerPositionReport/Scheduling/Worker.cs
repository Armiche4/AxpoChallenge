using Microsoft.Extensions.Options;
using PowerPositionReport.Configuration;
using PowerPositionReport.Extraction;

namespace PowerPositionReport.Scheduling;

/// <summary>
/// Decides WHEN an extraction runs; <see cref="IPowerPositionReportService"/> decides WHAT one
/// does. That split is why this class knows nothing about the trading system, the aggregation
/// or the CSV file (single responsibility principle).
/// </summary>
public class Worker(
    ILogger<Worker> logger,
    IOptions<AppSettings> options,
    IPowerPositionReportService reportService) : BackgroundService
{
    // Captured once at construction (IOptions<T>, not IOptionsMonitor<T>): this worker
    // deliberately does not support picking up configuration changes at runtime.
    private readonly AppSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Worker starting. OutputPath={OutputPath}, IntervalMinutes={IntervalMinutes}",
            _settings.OutputPath,
            _settings.IntervalMinutes);

        // PeriodicTimer over Task.Delay in a loop: each period is measured from the moment
        // the previous tick completed, so waiting does not accumulate drift relative to wall
        // clock across a long-running process. It is also the cancellation-aware primitive
        // recommended for exactly this "tick every X minutes" pattern in a BackgroundService.
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.IntervalMinutes));

        try
        {
            // Requirement 8: an extract must run as soon as the application starts, and only
            // then at every configured interval - so this first run happens before the loop
            // starts waiting on the timer below.
            await RunExtractionSafelyAsync(stoppingToken);

            // WaitForNextTickAsync returns false only if the timer was disposed while someone
            // was waiting on it; it throws OperationCanceledException if stoppingToken fires
            // while waiting - both are handled below.
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunExtractionSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down (host cancelled stoppingToken). This is
            // expected and not an error - fall through to the log line below and exit.
        }

        logger.LogInformation("Worker stopping.");
    }

    /// <summary>
    /// Runs a single scheduled extraction, catching and logging any exception raised by it.
    /// Requirement 7: it is not acceptable to miss a scheduled extract. If one extraction
    /// fails (e.g. an Axpo.PowerServiceException from the trading system, or a disk error
    /// writing the CSV), the exception must not escape this method: doing so would unwind
    /// ExecuteAsync, kill the PeriodicTimer, and stop every future scheduled run along with
    /// it. Logging the failure here and returning normally keeps the loop - and the next
    /// scheduled extraction - alive. The one exception that IS allowed to escape is a genuine
    /// shutdown request (stoppingToken cancelled), which the caller needs to see in order to
    /// stop the loop instead of scheduling yet another run.
    /// </summary>
    private async Task RunExtractionSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            await reportService.RunExtractionAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled extraction failed. It will be retried at the next scheduled interval.");
        }
    }
}
