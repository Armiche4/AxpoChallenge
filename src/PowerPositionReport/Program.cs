using Axpo;
using PowerPositionReport;
using PowerPositionReport.Configuration;

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
// Registers PowerService (the provided DLL) as the implementation for IPowerService.
// Using the interface allows us to easily replace it with a mock during testing.
builder.Services.AddSingleton<IPowerService, PowerService>();

// --- Worker ---
// Registers the Worker as a background service that executes automatically upon application startup.
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
