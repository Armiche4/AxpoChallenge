using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PowerPositionReport.Configuration;
using PowerPositionReport.Extraction;

namespace PowerPositionReport.Test;


public class WorkerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task RunsAnExtractionOnStartup_WithoutWaitingForTheFirstInterval()
    {
        // Requirement 8. The interval is 60 minutes, so a worker that waited for the first
        // timer tick would never call the orchestrator and this test would time out.
        var reportService = new FakeReportService();
        using var worker = CreateWorker(reportService);

        await worker.StartAsync(CancellationToken.None);
        await reportService.FirstCall.WaitAsync(Timeout);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, reportService.CallCount);
    }

    [Fact]
    public async Task StaysAlive_WhenAnExtractionThrows()
    {
        // Requirement 7: a failed extraction must not unwind ExecuteAsync, because that would
        // dispose the PeriodicTimer and cancel every future scheduled extraction.
        var reportService = new FakeReportService
        {
            Failure = new InvalidOperationException("trading system down"),
        };
        using var worker = CreateWorker(reportService);

        await worker.StartAsync(CancellationToken.None);
        await reportService.FirstCall.WaitAsync(Timeout);

        // A worker that let the exception escape would finish within this window.
        await Task.Delay(200);

        // Not completed = still in the loop, waiting on the timer for the next extraction.
        Assert.False(worker.ExecuteTask!.IsCompleted, "The worker must survive a failed extraction.");

        await worker.StopAsync(CancellationToken.None);
    }

    // 60 minutes: long enough that no second tick can happen while a test is running.
    private static Worker CreateWorker(IPowerPositionReportService reportService) =>
        new(NullLogger<Worker>.Instance,
            Options.Create(new AppSettings { OutputPath = Path.GetTempPath(), IntervalMinutes = 60 }),
            reportService);

    private sealed class FakeReportService : IPowerPositionReportService
    {
        // Lets a test await the first extraction instead of sleeping and hoping.
        private readonly TaskCompletionSource _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public Exception? Failure { get; init; }

        public Task FirstCall => _firstCall.Task;

        public Task RunExtractionAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            _firstCall.TrySetResult();

            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
