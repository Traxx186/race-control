using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl;
using RaceControl.Database;
using RaceControl.Jobs;
using RaceControl.Track;
using Serilog;
using Serilog.Settings.Configuration;

var app = SetupWebApplication(args);
app.UseForwardedHeaders();
app.UseWebSockets();

var trackStatus = app.Services.GetRequiredService<TrackStatus>();
trackStatus.OnTrackFlagChange += flagData => Broadcast(flagData).Wait();

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
    await SendFlag(webSocket, trackStatus.ActiveFlag);

    while (!CancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
    { 
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken);
        if (result.MessageType != WebSocketMessageType.Close)
            continue;

        Log.Information("[Race Control] User disconnected, removing from open connection list");
        Connections.Remove(webSocket);
    }
});

Log.Information("[Race Control] Starting Application");
await app.RunAsync();

static WebApplication SetupWebApplication(string[] args)
{
    var builder = WebApplication.CreateSlimBuilder(args);
    builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
    
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(
            builder.Configuration,
            new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles)
        )
        .CreateLogger();
    
    builder.Services.AddSerilog();
    builder.Services.AddSingleton<TrackStatus>();
    
    // Create DB Context pool.
    builder.Services.AddDbContextPool<RaceControlContext>(options =>
        options
            .UseNpgsql(builder.Configuration["DATABASE_URL"])
            .UseSnakeCaseNamingConvention()
    );

    // Add Quartz to services
    builder.Services.AddQuartz(quartz => 
    {
        var syncJobKey = new JobKey("SyncSessionsJob");
        quartz.AddJob<SyncSessionsJob>(opts => opts.WithIdentity(syncJobKey));
        quartz.AddTrigger(opts => opts
            .ForJob(syncJobKey)
            .WithIdentity("SyncSessionsJob-trigger")
            .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Thursday, 8, 0))
        );
        
        var fetchSessionJobKey = new JobKey("FetchActiveSessionJob");
        quartz.AddJob<FetchActiveSessionJob>(opts => opts.WithIdentity(fetchSessionJobKey));
        quartz.AddTrigger(opts => opts
            .ForJob(fetchSessionJobKey)
            .WithIdentity("FetchActiveSessionJob-trigger")
            .WithCronSchedule("0 * * ? * SUN,THU,FRI,SAT")
        );
    });

    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    builder.Services.AddHostedService<CategoryService>();
    
    return builder.Build();
}

// Sends a message to all connected clients.
static async Task Broadcast(FlagData flagData)
{
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