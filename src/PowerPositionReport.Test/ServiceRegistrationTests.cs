using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PowerPositionReport.Aggregation;
using PowerPositionReport.Configuration;
using PowerPositionReport.Extraction;
using PowerPositionReport.Reporting;
using PowerPositionReport.Scheduling;

namespace PowerPositionReport.Test;

/// <summary>
/// Smoke test for the dependency injection wiring in
/// <see cref="ServiceCollectionExtensions.AddPowerPositionReportServices"/>. Unit tests build
/// their subjects by hand, so nothing else would notice a forgotten registration - the
/// application would simply fail at startup instead. This test catches that in the build.
/// </summary>
public class ServiceRegistrationTests
{
    [Fact]
    public void EveryServiceTheWorkerDependsOn_CanBeResolvedFromTheContainer()
    {
        // Mirrors Program.cs: same configuration + registrations, without starting a host.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<AppSettings>().Configure(settings =>
        {
            settings.OutputPath = Path.GetTempPath();
            settings.IntervalMinutes = 5;
        });

        services.AddPowerPositionReportServices();
        services.AddHostedService<Worker>();

        // ValidateOnBuild walks the whole dependency graph up front: if any of the services
        // above needed something that was never registered, this throws here.
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.NotNull(provider.GetRequiredService<IPowerPositionAggregator>());
        Assert.NotNull(provider.GetRequiredService<ICsvReportWriter>());
        Assert.NotNull(provider.GetRequiredService<IPowerPositionReportService>());

        // The Worker is registered as the one and only hosted service.
        Assert.IsType<Worker>(Assert.Single(provider.GetServices<IHostedService>()));
    }
}
