using Axpo;
using Microsoft.Extensions.Options;
using PowerPositionReport.Aggregation;
using PowerPositionReport.Configuration;
using PowerPositionReport.Reporting;

namespace PowerPositionReport.Extraction;

/// <summary>
/// Implementation of <see cref="IPowerPositionReportService"/>. Wires together
/// <see cref="IPowerService"/> (data source), <see cref="IPowerPositionAggregator"/>
/// (calculation) and <see cref="ICsvReportWriter"/> (output) into a single extraction.
/// </summary>
public class PowerPositionReportService(
    IPowerService powerService,
    IPowerPositionAggregator aggregator,
    ICsvReportWriter csvReportWriter,
    IOptions<AppSettings> options,
    ILogger<PowerPositionReportService> logger) : IPowerPositionReportService
{
    private readonly AppSettings _settings = options.Value;

    public async Task RunExtractionAsync(CancellationToken cancellationToken)
    {
        // "Now", in Europe/London local time - not server time, which in production is likely
        // UTC. See SOLUTION.md for why "tomorrow" (day-ahead) is computed this way.
        var londonNow = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, LondonTimeZone.Instance);
        var deliveryDate = londonNow.Date.AddDays(1);

        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Extraction starting. DeliveryDate={DeliveryDate:yyyy-MM-dd}, ExtractionLocalTime={ExtractionLocalTime:yyyy-MM-dd HH:mm:ss}, OutputPath={OutputPath}.",
            deliveryDate,
            londonNow.DateTime,
            _settings.OutputPath);

        // Day-ahead position: PowerService.GetTradesAsync(date) returns the block of periods
        // [23:00 on date-1, 23:00 on date), so passing tomorrow's date gives the position for
        // the next full trading day, per the challenge's "day ahead power position" wording.
        var trades = await powerService.GetTradesAsync(deliveryDate);

        var hourlyVolumes = aggregator.Aggregate(trades);

        logger.LogInformation(
            "Aggregation complete for delivery date {DeliveryDate:yyyy-MM-dd}. HourlyRows={HourlyRowCount}, TotalVolume={TotalVolume}.",
            deliveryDate,
            hourlyVolumes.Count,
            hourlyVolumes.Sum(hourlyVolume => hourlyVolume.Volume));

        // The CSV filename records the local time of extract (i.e. now, not the delivery date).
        var filePath = csvReportWriter.Write(hourlyVolumes, _settings.OutputPath, londonNow.DateTime);

        logger.LogInformation(
            "Extraction complete for delivery date {DeliveryDate:yyyy-MM-dd}. Report written to {FilePath}.",
            deliveryDate,
            filePath);
    }
}
