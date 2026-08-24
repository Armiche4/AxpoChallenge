using Microsoft.Extensions.Options;
using PowerPositionReport.Configuration;
using PowerPositionReport.Extraction;

namespace PowerPositionReport;

/// <summary>
/// Decides WHEN an extraction runs; <see cref="IPowerPositionReportService"/> decides WHAT one
/// does. 
/// </summary>
public class Worker(
    ILogger<Worker> logger,
    IOptions<AppSettings> options,
    IPowerPositionReportService reportService) : BackgroundService
{
    
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
                logger.LogError(ex, "Scheduled extraction failed. It will be retried at the next scheduled interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
