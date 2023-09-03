using System.Net;
using System.Net.WebSockets;
using System.Text;
using dotenv.net;
using Newtonsoft.Json;
using RaceControl;
using RaceControl.Track;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

SetupLogging();
DotEnv.Load();

var cancellationToken = SetupGracefulShutdown();
var trackStatus = new TrackStatus();
var category = new CategoryService();
category.OnCategoryFlagChange += data => trackStatus.SetActiveFlag(data);

var appUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5050";
var app = SetupWebApplication(args);

app.UseForwardedHeaders();
app.UseWebSockets();
app.Map("/", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    while (!cancellationToken.IsCancellationRequested)
    {
        trackStatus.OnTrackFlagChange += flagData => SendFlag(webSocket, flagData, cancellationToken).Wait();
        Console.Read();
    }
});

Log.Information("[Race Control] Starting category service & WebSocket server");
Task.WaitAll(
    Task.Run(() => category.Start()),
    Task.Run(() => app.Run(appUrl))
);

static void SetupLogging()
{
    var executingDir = Path.GetDirectoryName(AppContext.BaseDirectory);
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

static CancellationToken SetupGracefulShutdown()
{
    var tokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, _) => tokenSource.Cancel();
    AppDomain.CurrentDomain.ProcessExit += (_, _) => tokenSource.Cancel();

    return tokenSource.Token;
}

static WebApplication SetupWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    return builder.Build();
}

static async Task SendFlag(WebSocket webSocket, FlagData flagData, CancellationToken cancellationToken)
{
    var json = JsonConvert.SerializeObject(flagData);
    var data = Encoding.UTF8.GetBytes(json); 
    
    Log.Information($"[Race Control] Sending flag '{flagData.Flag}' to all connected clients"); 
    await webSocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
}