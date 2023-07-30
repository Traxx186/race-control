using System.Reflection;
using RaceControl.Category;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

InitLogging();

var formula1 = new Formula1("https://livetiming.formula1.com");
formula1.OnFlagParsed += data => { Console.WriteLine(data.Flag); };

formula1.Start();

static void InitLogging()
{
    var executingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
    var logPath = Path.Combine(executingDir ?? string.Empty, "logs", "verbose.log");

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)
        .WriteTo.Console(
            theme: AnsiConsoleTheme.Literate,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message} {NewLine}{Exception}"
        )
        .CreateLogger();
}