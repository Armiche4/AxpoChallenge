using System.ComponentModel.DataAnnotations;

namespace PowerPositionReport.Configuration;

/// <summary>
/// Configuration App model
/// Values are automatically read from the 'AppSettings' section of appsettings.json,
/// and can be overridden via command-line arguments
/// (e.g. --AppSettings:OutputPath=... --AppSettings:IntervalMinutes=...).
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Folder path where the generated CSVs will be saved.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "AppSettings:OutputPath must be set to a valid folder path.")]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Interval in minutes between each report extraction.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "AppSettings:IntervalMinutes must be a positive number of minutes.")]
    public int IntervalMinutes { get; set; } = 5;
}
