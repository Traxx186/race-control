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
trackStatus.OnTrackFlagChange += flagData => Broadcast(flagData, CancellationToken.None).Wait();

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
    await SendFlag(webSocket, trackStatus.ActiveFlag, context.RequestAborted);

    while (!context.RequestAborted.IsCancellationRequested && webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(buffer, context.RequestAborted);
        if (result.MessageType != WebSocketMessageType.Close)
            continue;

        Log.Information("[Race Control] User disconnected, removing from open connection list");
        Connections.Remove(webSocket);
    }
});

Log.Information("[Race Control] Starting Application");
await app.RunAsync();

public partial class Program
{
    /// <summary>
    /// All the connected web socket clients.
    /// </summary>
    private static readonly List<WebSocket> Connections = [];

    /// <summary>
    /// Create the main web application with the required configuration and linked services
    /// </summary>
    /// <param name="args">Application arguments.</param>
    /// <returns>The web application.</returns>
    private static WebApplication SetupWebApplication(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        builder.Configuration.SetBasePath(Environment.CurrentDirectory);
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
        builder.Services.AddSingleton<CategoryService>();

        // Create DB Context pool.
        builder.Services.AddDbContextPool<RaceControlContext>(opts => opts
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
                    .WithCronSchedule("0 0 8 ? * THU")
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

        return builder.Build();
    }

    /// <summary>
    /// Sends a websocket response to all the connected client.
    /// </summary>
    /// <param name="flagData">Flag data to send.</param>
    /// <param name="token">If the response needs to be cancelled.</param>
    private static async Task Broadcast(FlagData flagData, CancellationToken token)
    {
        Log.Information($"[Race Control] Sending flag '{flagData.Flag}' to all connected clients");
        var openSockets = Connections.Where(x => x.State == WebSocketState.Open);
        foreach (var websocket in openSockets)
            await SendFlag(websocket, flagData, token);
    }

    /// <summary>
    /// Send a websocket message with flag data
    /// </summary>
    /// <param name="websocket">The websocket client.</param>
    /// <param name="flagData">Flag data to send.</param>
    /// <param name="cancellationToken">If the response needs to be cancelled.</param>
    private static async Task SendFlag(WebSocket websocket, FlagData flagData, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(flagData);
        var data = Encoding.UTF8.GetBytes(json);

        await websocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
    }
}