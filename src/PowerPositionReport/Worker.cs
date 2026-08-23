using Microsoft.Extensions.Options;
using PowerPositionReport.Configuration;
using PowerPositionReport.Extraction;

namespace PowerPositionReport;

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

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.IntervalMinutes));

       
        // the first wait, and every later one after a tick.
        do
        {
            try
            {
                await reportService.RunExtractionAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Requirement 7: it is not acceptable to miss a scheduled extract. If this
                // exception escaped, it would end ExecuteAsync, dispose the timer and cancel
                // every future extraction. Logging it here loses one run, not the schedule.
                // The "when" filter lets shutdown cancellations through: those are not errors
                // and must stop the loop.
                logger.LogError(ex, "Scheduled extraction failed. It will be retried at the next scheduled interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
