using System.Reflection;
using RaceControl.SignalR;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

InitLogging();

var client = new Client(
    "https://livetiming.formula1.com",
    "Streaming",
    new[] { "RaceControlMessages", "TrackStatus" }
);

await client.Start();

static void InitLogging()
{
    var executingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
    var logPath = Path.Combine(executingDir ?? string.Empty, "logs", "verbose.log");

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
        .WriteTo.Console(
            theme: AnsiConsoleTheme.Literate,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message} {NewLine}{Exception}"
        )
        .CreateLogger();
}