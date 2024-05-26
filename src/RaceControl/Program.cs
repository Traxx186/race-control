using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RaceControl;
using RaceControl.Track;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

SetupLogging();

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
    flagData = (FlagData)flagData.Clone();
    if (null == flagData)
        return;

    Log.Information($"[Race Control] Sending flag '{flagData.Flag}' to all connected clients");
    var openSockets = connections.Where(x => x.State == WebSocketState.Open);
    foreach (var websocket in openSockets)
        await SendFlag(websocket, flagData, cancellationToken);
}

// Serialize the flag data to json and send the json to the client.
static async Task SendFlag(WebSocket websocket, FlagData flagData, CancellationToken cancellationToken)
{
    var json = JsonSerializer.Serialize(flagData);
    var data = Encoding.UTF8.GetBytes(json);

    await websocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
}