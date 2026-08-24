using System.Diagnostics;
using Axpo;

namespace PowerPositionReport.Extraction;

/// <summary>
/// Retries the trading system when it fails, so one transient error does not cost a whole
/// scheduled extract.
///
/// The budget is a deadline, not a number of attempts.
/// </summary>
public class RetryingPowerService(
    IPowerService powerService,
    ILogger<RetryingPowerService> logger,
    TimeSpan retryDelay,
    TimeSpan retryBudget) : IPowerService
{
    public async Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date)
    {
        var elapsed = Stopwatch.StartNew();

        for (var retry = 1; ; retry++)
        {
            try
            {
                return await powerService.GetTradesAsync(date);
            }

            catch (PowerServiceException ex) when (elapsed.Elapsed + retryDelay < retryBudget)
            {
                logger.LogWarning(
                    ex,
                    "Trading system call failed. Retry {Retry}, {Elapsed:F1}s of the {Budget:F0}s budget used.",
                    retry,
                    elapsed.Elapsed.TotalSeconds,
                    retryBudget.TotalSeconds);

                await Task.Delay(retryDelay);
            }
        }
    }

    // This one is here because the interface has it.
    public IEnumerable<PowerTrade> GetTrades(DateTime date) => powerService.GetTrades(date);
}
