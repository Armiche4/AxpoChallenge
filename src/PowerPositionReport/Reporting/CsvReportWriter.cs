using System.Globalization;
using PowerPositionReport.Aggregation;

namespace PowerPositionReport.Reporting;


public class CsvReportWriter : ICsvReportWriter
{
    private const string HeaderRow = "Local Time,Volume";

    // "PowerPosition_20141220_1837.csv" style.
    private const string FileNameTimestampFormat = "yyyyMMdd_HHmm";

    // 24-hour clock, e.g. "13:00".
    private const string LocalTimeFormat = "HH:mm";

    public string Write(IReadOnlyList<HourlyVolume> hourlyVolumes, string outputPath, DateTime extractionLocalTime)
    {
        ArgumentNullException.ThrowIfNull(hourlyVolumes);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);


        Directory.CreateDirectory(outputPath);

        var fileName = $"PowerPosition_{extractionLocalTime.ToString(FileNameTimestampFormat, CultureInfo.InvariantCulture)}.csv";
        var filePath = Path.Combine(outputPath, fileName);
        // append false removes all content previous
        using (var writer = new StreamWriter(filePath, append: false))
        {
            writer.WriteLine(HeaderRow);

            foreach (var hourlyVolume in hourlyVolumes)
            {
                var localTime = hourlyVolume.LocalHour.ToString(LocalTimeFormat, CultureInfo.InvariantCulture);
                var volume = hourlyVolume.Volume.ToString(CultureInfo.InvariantCulture);
                writer.WriteLine($"{localTime},{volume}");
            }
        }

        return filePath;
    }
}
