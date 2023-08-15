using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RaceControl.Category;
using RaceControl.Track;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

InitLogging();

var formula1 = new Formula1("https://livetiming.formula1.com");
var tokenSource = new CancellationTokenSource();
var token = tokenSource.Token;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.Map("/", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    while (!token.IsCancellationRequested)
    {
        formula1.OnFlagParsed += flagData => SendFlag(webSocket, flagData, token).Wait();
        Console.Read();
    }
});

var tasks = new List<Task>
{
    Task.Run(() => formula1.Start()),
    Task.Run(() => app.Run("http://localhost:8567"))
};

Task.WaitAll(tasks.ToArray());

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

static async Task SendFlag(WebSocket webSocket, FlagData flagData, CancellationToken cancellationToken)
{
    var json = JsonConvert.SerializeObject(flagData);
    var data = Encoding.UTF8.GetBytes(json);

    await webSocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
}