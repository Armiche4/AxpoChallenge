using Axpo;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PowerPositionReport.Aggregation;
using PowerPositionReport.Configuration;
using PowerPositionReport.Extraction;
using PowerPositionReport.Reporting;

namespace PowerPositionReport.Test;

public class PowerPositionReportServiceTests
{
    private const string ConfiguredOutputPath = @"C:\Reports";


    [Fact]
    public async Task RunExtractionAsync_FeedsTheTradesToTheAggregator_AndItsResultToTheWriter()
    {
        var trade = PowerTrade.Create(new DateTime(2015, 1, 4), 24);
        var aggregatedVolumes = new List<HourlyVolume> { new(new TimeOnly(23, 0), 150) };

        var aggregator = new FakeAggregator(aggregatedVolumes);
        var writer = new FakeCsvReportWriter();

        await CreateService(new FakePowerService(trades: [trade]), aggregator, writer)
            .RunExtractionAsync(CancellationToken.None);

        Assert.Same(trade, Assert.Single(aggregator.ReceivedTrades!));
        Assert.Same(aggregatedVolumes, writer.ReceivedHourlyVolumes);
        Assert.Equal(ConfiguredOutputPath, writer.ReceivedOutputPath);
    }

    [Fact]
    public async Task RunExtractionAsync_PropagatesFailures_AndWritesNoReport()
    {

        var powerService = new FakePowerService(failure: new InvalidOperationException("trading system down"));
        var writer = new FakeCsvReportWriter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(powerService, new FakeAggregator(), writer).RunExtractionAsync(CancellationToken.None));

        Assert.Null(writer.ReceivedHourlyVolumes);
    }

    private static PowerPositionReportService CreateService(
        FakePowerService powerService,
        FakeAggregator aggregator,
        FakeCsvReportWriter writer) =>
        new(powerService,
            aggregator,
            writer,
            Options.Create(new AppSettings { OutputPath = ConfiguredOutputPath }),
            NullLogger<PowerPositionReportService>.Instance);

    // --- Fakes: each one records what it was given and returns a preset answer. ---

    private class FakePowerService(IEnumerable<PowerTrade>? trades = null, Exception? failure = null) : IPowerService
    {
        public DateTime RequestedDate { get; private set; }

        public IEnumerable<PowerTrade> GetTrades(DateTime date) => throw new NotSupportedException();

        public Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date)
        {
            RequestedDate = date;

            return failure is null
                ? Task.FromResult(trades ?? Enumerable.Empty<PowerTrade>())
                : Task.FromException<IEnumerable<PowerTrade>>(failure);
        }
    }

    private class FakeAggregator(IReadOnlyList<HourlyVolume>? result = null) : IPowerPositionAggregator
    {
        public IEnumerable<PowerTrade>? ReceivedTrades { get; private set; }

        public IReadOnlyList<HourlyVolume> Aggregate(IEnumerable<PowerTrade> trades)
        {
            ReceivedTrades = trades;
            return result ?? [];
        }
    }

    private class FakeCsvReportWriter : ICsvReportWriter
    {
        // Stays null when no report was written.
        public IReadOnlyList<HourlyVolume>? ReceivedHourlyVolumes { get; private set; }

        public string? ReceivedOutputPath { get; private set; }

        public string Write(IReadOnlyList<HourlyVolume> hourlyVolumes, string outputPath, DateTime extractionLocalTime)
        {
            ReceivedHourlyVolumes = hourlyVolumes;
            ReceivedOutputPath = outputPath;

            return Path.Combine(outputPath, "PowerPosition_fake.csv");
        }
    }
}
