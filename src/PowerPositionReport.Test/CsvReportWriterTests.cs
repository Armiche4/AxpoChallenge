using PowerPositionReport.Aggregation;
using PowerPositionReport.Reporting;

namespace PowerPositionReport.Test;

public class CsvReportWriterTests : IDisposable
{
    private readonly string _outputPath;

    public CsvReportWriterTests()
    {
        //unique folder per test run so tests don't interfere with each other  
        _outputPath = Path.Combine(Path.GetTempPath(), "PowerPositionReportTests_" + Guid.NewGuid());
    }

    // Clean up the temporary output folder after each test run
    public void Dispose()
    {       
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, recursive: true);
        }
    }

    [Fact]
    public void Write_CreatesFileWithHeaderAndRows_UsingReadmeExampleData()
    {
        var hourlyVolumes = new List<HourlyVolume>
        {
            new(new TimeOnly(23, 0), 150),
            new(new TimeOnly(0, 0), 150),
            new(new TimeOnly(10, 0), 80),
            new(new TimeOnly(22, 0), -20.5),
        };
        var extractionLocalTime = new DateTime(2014, 12, 20, 18, 37, 0);

        var writer = new CsvReportWriter();
        var filePath = writer.Write(hourlyVolumes, _outputPath, extractionLocalTime);

        Assert.Equal(Path.Combine(_outputPath, "PowerPosition_20141220_1837.csv"), filePath);
        Assert.True(File.Exists(filePath));

        var lines = File.ReadAllLines(filePath);
        var expected = new[]
        {
            "Local Time,Volume",
            "23:00,150",
            "00:00,150",
            "10:00,80",
            "22:00,-20.5",
        };
        Assert.Equal(expected, lines);
    }

    [Fact]
    public void Write_CreatesOutputFolder_WhenItDoesNotExist()
    {
        Assert.False(Directory.Exists(_outputPath));

        var writer = new CsvReportWriter();
        writer.Write(new List<HourlyVolume>(), _outputPath, new DateTime(2014, 12, 20, 18, 37, 0));

        Assert.True(Directory.Exists(_outputPath));
    }

    [Fact]
    public void Write_UsesExtractionLocalTime_NotUtcNow_ForTheFileName()
    {
        //  the filename must be built from the extraction's London time      
        var extractionLocalTime = new DateTime(2026, 6, 21, 1, 5, 0); // London time close to midnight,
        // chosen so that if UTC were used by mistake the date part would shift and the test
        // would fail (Europe/London is ahead of UTC around the summer solstice).

        var writer = new CsvReportWriter();
        var filePath = writer.Write(new List<HourlyVolume>(), _outputPath, extractionLocalTime);

        Assert.Equal(Path.Combine(_outputPath, "PowerPosition_20260621_0105.csv"), filePath);
    }
}
