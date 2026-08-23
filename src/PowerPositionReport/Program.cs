using PowerPositionReport.Configuration;
using PowerPositionReport.Scheduling;

var builder = Host.CreateApplicationBuilder(args);

// --- Configuration ---
// Reads the "AppSettings" section from appsettings.json (or command-line overrides,
// e.g. --AppSettings:OutputPath=... --AppSettings:IntervalMinutes=...), binds it to
// the AppSettings class.
builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration.GetSection("AppSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// --- Services ---
// Every interface -> implementation registration lives in
// Configuration/ServiceCollectionExtensions.cs, so this file stays a short composition root
// instead of growing with every new service.
builder.Services.AddPowerPositionReportServices();

// --- Worker ---
// Registers the Worker as a background service that executes automatically upon application startup.
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
