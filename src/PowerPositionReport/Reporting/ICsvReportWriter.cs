using PowerPositionReport.Aggregation;

namespace PowerPositionReport.Reporting;

/// <summary>
/// Writes a set of hourly volumes out as a CSV report.
/// </summary>
public interface ICsvReportWriter
{
    /// <summary>
    /// Writes <paramref name="hourlyVolumes"/> to a CSV file inside <paramref name="outputPath"/>.
    /// The file is named PowerPosition_YYYYMMDD_HHMM.csv, where the date/time is
    /// <paramref name="extractionLocalTime"/> — the LOCAL time the extract was taken not UTC.
    /// <paramref name="outputPath"/> is created if it does not exist.
    /// </summary>
    /// <returns>The full path of the file that was written.</returns>
    string Write(IReadOnlyList<HourlyVolume> hourlyVolumes, string outputPath, DateTime extractionLocalTime);
}
