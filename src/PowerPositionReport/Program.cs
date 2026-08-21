using Axpo;
using PowerPositionReport;
using PowerPositionReport.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// --- Configuration ---
// Reads the "AppSettings" section from appsettings.json and binds it to the AppSettings class.
// This allows IOptions<AppSettings> to be injected into any service.
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// --- Services ---
// Registers PowerService (the provided DLL) as the implementation for IPowerService.
// Using the interface allows us to easily replace it with a mock during testing.
builder.Services.AddSingleton<IPowerService, PowerService>();

// --- Worker ---
// Registers the Worker as a background service that executes automatically upon application startup.
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
