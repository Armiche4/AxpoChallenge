using Axpo;
using PowerPositionReport.Aggregation;
using PowerPositionReport.Extraction;
using PowerPositionReport.Reporting;

namespace PowerPositionReport.Configuration;

/// <summary>
/// Registers every service PowerPositionReport needs, in one place, so <c>Program.cs</c> stays
/// a short composition root instead of growing a long list of individual AddSingleton calls as
/// the app gets more pieces.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers each interface against its single implementation (dependency inversion
    /// principle): every class in the app depends only on the interface, never on how it is
    /// wired up here, and can be swapped for a test double without touching the class itself.
    /// All singletons: none of them holds mutable state, so one instance is reused by every
    /// extraction.
    /// </summary>
    public static IServiceCollection AddPowerPositionReportServices(this IServiceCollection services)
    {
        // PowerService (the provided DLL) is the implementation for IPowerService.
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<IPowerPositionAggregator, PowerPositionAggregator>();
        services.AddSingleton<ICsvReportWriter, CsvReportWriter>();
        services.AddSingleton<IPowerPositionReportService, PowerPositionReportService>();

        return services;
    }
}
