using Microsoft.Extensions.Options;
using PowerPositionReport.Configuration;

namespace PowerPositionReport;

public class Worker(ILogger<Worker> logger, IOptions<AppSettings> options) : BackgroundService
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

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
