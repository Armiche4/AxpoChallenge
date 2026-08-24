using PowerPositionReport;
using PowerPositionReport.Configuration;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// --- Logging ---
builder.Services.AddSerilog(config => config
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "powerpositionreport-.log"),
        rollingInterval: RollingInterval.Day));

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
builder.Services.AddPowerPositionReportServices();

// --- Worker ---
// Registers the Worker as a background service that executes automatically upon application startup.
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
