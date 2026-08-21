namespace PowerPositionReport.Configuration;

/// <summary>
/// Configuration App model
/// Values are automatically read from the 'AppSettings' section of appsettings.json.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Folder path where the generated CSVs will be saved..
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Interval in minutes between each report extraction.
    /// </summary>
    public int IntervalMinutes { get; set; } = 5;
}
