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
            {
                await BroadcastFlag();
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Flag Broadcast Service] Stopping");
        }
    }

    /// <summary>
    /// Broadcasts a flag to the connected clients when the enqueue time is more than the latency of the active
    /// category.
    /// </summary>
    private async Task BroadcastFlag()
    {
        var currentCategory = categoryService.ActiveSession?.Category;
        if (currentCategory is null)
            return;

        var queryTime = DateTime.UtcNow.AddSeconds(currentCategory.Latency);
        var flagToBroadcast = categoryService.FlagQueue.FirstOrDefault(q => q.Key >= queryTime).Value;
        if (flagToBroadcast == null)
            return;

        logger.LogInformation("[Flag Broadcast Service] Broadcasting flag {flag}", flagToBroadcast.Flag);
        await trackStatusService.SetActiveFlagAsync(flagToBroadcast.Flag, flagToBroadcast.Driver);
    }
}