using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dotenv.net;
using RaceControl;
using RaceControl.Track;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

SetupLogging();
DotEnv.Load();

var cancellationToken = SetupGracefulShutdown();
var connections = new List<WebSocket>();

var trackStatus = new TrackStatus();
trackStatus.OnTrackFlagChange += flagData => Broadcast(connections, flagData, cancellationToken).Wait();

var categoryService = new CategoryService();
categoryService.OnCategoryFlagChange += trackStatus.SetActiveFlag;

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

    var buffer = new byte[4096];
    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    connections.Add(webSocket);

    Log.Information("[Race Control] New user connected, sending current active flag");
    await SendFlag(webSocket, trackStatus.ActiveFlag, cancellationToken);

    while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
    { 
        var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
        if (result.MessageType != WebSocketMessageType.Close)
            continue;

        Log.Information("[Race Control] User disconnected, removing from open connection list");
        connections.Remove(webSocket);
    }
});

Log.Information("[Race Control] Starting category service & WebSocket server");
Task.WaitAll(
    Task.Run(categoryService.Start),
    Task.Run(app.Run)
);

// Setup Serilog
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

// Creates a CancellationTokenSource to allow for a graceful shutdow
static CancellationToken SetupGracefulShutdown()
{
    var tokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, _) => tokenSource.Cancel();
    AppDomain.CurrentDomain.ProcessExit += (_, _) => tokenSource.Cancel();

    return tokenSource.Token;
}

static WebApplication SetupWebApplication(string[] args)
{
    var builder = WebApplication.CreateSlimBuilder(args);

    return builder.Build();
}

// Sends a message to all connected clients.
static async Task Broadcast(List<WebSocket> connections, FlagData flagData, CancellationToken cancellationToken)
{
    flagData = flagData.Clone() as FlagData;
    if (null == flagData)
        return;

    await Task.Delay(35_000, cancellationToken);
    Log.Information($"[Race Control] Sending flag '{flagData.Flag}' to all connected clients");

    foreach (var websocket in connections)
    {
        if (websocket.State == WebSocketState.Open)
            await SendFlag(websocket, flagData, cancellationToken);
    }
}

// Send the parsed FlagData to the client.
static async Task SendFlag(WebSocket websocket, FlagData flagData, CancellationToken cancellationToken)
{
    var json = JsonSerializer.Serialize(flagData);
    var data = Encoding.UTF8.GetBytes(json);

    await websocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
}