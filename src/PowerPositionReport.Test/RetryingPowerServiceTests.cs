using Axpo;
using Microsoft.Extensions.Logging.Abstractions;
using PowerPositionReport.Extraction;

namespace PowerPositionReport.Test;


public class RetryingPowerServiceTests
{
    [Fact]
    public async Task GetTradesAsync_GivesUpAndPropagates_WhenTheBudgetRunsOut()
    {
        var tradingSystem = new FailsPowerService(failedCalls: int.MaxValue);
        var retrying = new RetryingPowerService(
            tradingSystem,
            NullLogger<RetryingPowerService>.Instance,
            retryDelay: TimeSpan.FromMilliseconds(10),
            retryBudget: TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<PowerServiceException>(() => retrying.GetTradesAsync(DateTime.Today));

        Assert.True(
            tradingSystem.CallCount >= 2,
            $"Expected more than one attempt, got {tradingSystem.CallCount}.");
    }

    // Fails its first failedCalls calls, then returns one trade.
    private class FailsPowerService(int failedCalls) : IPowerService
    {
        public int CallCount { get; private set; }

        public Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date) =>
            ++CallCount <= failedCalls
                ? throw new PowerServiceException("Error retrieving power volumes")
                : Task.FromResult<IEnumerable<PowerTrade>>([PowerTrade.Create(date, 24)]);

        public IEnumerable<PowerTrade> GetTrades(DateTime date) => throw new NotSupportedException();
    }
}
