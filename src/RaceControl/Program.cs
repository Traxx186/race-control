using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using RaceControl;
using RaceControl.Track;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

SetupLogging();

TrackStatus.OnTrackFlagChange += flagData => Broadcast(flagData).Wait();

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
    Connections.Add(webSocket);

    Log.Information("[Race Control] New user connected, sending current active flag");
    await SendFlag(webSocket, TrackStatus.ActiveFlag);

    while (!CancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
    { 
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken);
        if (result.MessageType != WebSocketMessageType.Close)
            continue;

        Log.Information("[Race Control] User disconnected, removing from open connection list");
        Connections.Remove(webSocket);
    }
});

Log.Information("[Race Control] Starting category service & WebSocket server");
await app.RunAsync();

// Setup Serilog
static void SetupLogging()
{
    var executingDir = Path.GetDirectoryName(Environment.CurrentDirectory);
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

static WebApplication SetupWebApplication(string[] args)
{
    var builder = WebApplication.CreateSlimBuilder(args);
    builder.Services.AddSerilog();

    // Add category service to application services.
    builder.Services.AddHostedService(ctx => 
    {
        var logger = ctx.GetRequiredService<ILogger<CategoryService>>();
        var categoryService = new CategoryService(logger);
        categoryService.CategoryFlagChange += (_, args) => TrackStatus.SetActiveFlag(args.FlagData);

        return categoryService;
    });

    return builder.Build();
}

// Sends a message to all connected clients.
static async Task Broadcast(FlagData flagData)
{
    flagData = (FlagData)flagData.Clone();
    if (null == flagData)
        return;

    Log.Information($"[Race Control] Sending flag '{flagData.Flag}' to all connected clients");
    var openSockets = Connections.Where(x => x.State == WebSocketState.Open);
    foreach (var websocket in openSockets)
        await SendFlag(websocket, flagData);
}

// Serialize the flag data to json and send the json to the client.
static async Task SendFlag(WebSocket websocket, FlagData flagData)
{
    var json = JsonSerializer.Serialize(flagData);
    var data = Encoding.UTF8.GetBytes(json);

    await websocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken);
}

public partial class Program 
{
    /// <summary>
    /// All the connected web socket clients.
    /// </summary>
    private static readonly List<WebSocket> Connections = [];

    /// <summary>
    /// Cancellation token for graceful shutdown.
    /// </summary>
    private static CancellationToken CancellationToken { get; } = SetupGracefulShutdown();

    /// <summary>
    /// Global instance of the TrackStatus.
    /// </summary>
    private static TrackStatus TrackStatus { get; } = new TrackStatus();

    /// <summary>
    /// Creates a new <see cref="CancellationToken"/> object for a graceful shutdown.
    /// </summary>
    /// <returns>A <see cref="CancellationToken"/> object.</returns>
    private static CancellationToken SetupGracefulShutdown()
    {
        var tokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, _) => tokenSource.Cancel();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => tokenSource.Cancel();

        return tokenSource.Token;
    }
}