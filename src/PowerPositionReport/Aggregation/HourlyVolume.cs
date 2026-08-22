namespace PowerPositionReport.Aggregation;

/// <summary>
/// Total volume aggregated for a specific local hour of the day.
/// This is the "business" data that the CSV writer will later turn into a row of the output file.
/// </summary>
/// <param name="LocalHour">The local hour (on the hour) the volume corresponds to, e.g. 23:00.</param>
/// <param name="Volume">Sum of the volumes of every period that falls within that local hour.</param>
public record HourlyVolume(TimeOnly LocalHour, double Volume);
