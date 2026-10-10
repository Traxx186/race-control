namespace RaceControl.Server.Services;

public sealed class FlagBroadcastService(
    ILogger<FlagBroadcastService> logger,
    IChampionshipService championshipService,
    ITrackStatusService trackStatusService)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[Flag Broadcast Service] Running.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await BroadcastFlag();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Flag Broadcast Service] Stopping");
        }
    }

    /// <summary>
    /// Broadcasts a flag to the connected clients.
    /// </summary>
    private async Task BroadcastFlag()
    {
        var currentChampionship = championshipService.ActiveSession?.Championship;
        if (currentChampionship is null)
            return;

        // Check if there is an entry present in the flag queue where the enqueue time plus the latency of the active
        // category.
        var flagToBroadcast = championshipService.FlagQueue.FirstOrDefault(q => DateTime.UtcNow - q.Key >= TimeSpan.FromSeconds(35));
        if (flagToBroadcast.Value == null)
            return;

        logger.LogInformation("[Flag Broadcast Service] Broadcasting flag {flag} with enqueue time of {time}", flagToBroadcast.Value.Flag, flagToBroadcast.Key.ToString("yyyy-MM-dd HH:mm:ss"));
        await trackStatusService.SetActiveFlagAsync(flagToBroadcast.Value);
        championshipService.FlagQueue.Remove(flagToBroadcast.Key);
    }
}