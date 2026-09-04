namespace RaceControl.Services;

public sealed class FlagBroadcastService(
    ILogger<FlagBroadcastService> logger,
    ICategoryService categoryService,
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
        var currentCategory = categoryService.ActiveSession?.Category;
        if (currentCategory is null)
            return;

        // Check if there is an entry present in the flag queue where the enqueue time plus the latency of the active
        // category.
        var flagToBroadcast = categoryService.FlagQueue.FirstOrDefault(q => DateTime.UtcNow - q.Key >= TimeSpan.FromSeconds(currentCategory.Latency));
        if (flagToBroadcast.Value == null)
            return;

        logger.LogInformation("[Flag Broadcast Service] Broadcasting flag {flag} with enqueue time of {time}", flagToBroadcast.Value.Flag, flagToBroadcast.Key.ToString("yyyy-MM-dd HH:mm:ss"));
        await trackStatusService.SetActiveFlagAsync(flagToBroadcast.Value.Flag, flagToBroadcast.Value.Driver);
        categoryService.FlagQueue.Remove(flagToBroadcast.Key);
    }
}