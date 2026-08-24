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
    /// Registers each interface against.
    /// </summary>
    public static IServiceCollection AddPowerPositionReportServices(this IServiceCollection services)
    {
        // Every consumer of IPowerService gets the retrying decorator, never the raw DLL client.
        // RetryingPowerService asks for the very interface it would be registered under.
        services.AddSingleton<IPowerService>(serviceProvider => new RetryingPowerService(
            new PowerService(),
            serviceProvider.GetRequiredService<ILogger<RetryingPowerService>>(),
            retryDelay: TimeSpan.FromSeconds(2),
            retryBudget: TimeSpan.FromSeconds(30)));

        services.AddSingleton<IPowerPositionAggregator, PowerPositionAggregator>();
        services.AddSingleton<ICsvReportWriter, CsvReportWriter>();
        services.AddSingleton<IPowerPositionReportService, PowerPositionReportService>();

        return services;
    }
}
