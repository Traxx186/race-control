using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Server.Services;

namespace RaceControl.Server.Hubs;

public class RaceControlHub(
    ITrackStatusService trackStatusService,
    ILogger<RaceControlHub> logger,
    ICategoryService categoryService) : Hub<IRaceControlHubClient>
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[RaceControlHub] New client connected, send flag data & current category if present");
        var category = categoryService.ActiveSession?.Category;
        var flagDataDto = new FlagDataDto(trackStatusService.ActiveFlag);

        if (category != null)
        {
            var categoryDto = new CategoryDto(category.Key, category.Latency);
            await Clients.Caller.CategoryChange(categoryDto);
            await Task.Delay(category.Latency * 1000);
        }

        await Clients.Caller.FlagChange(flagDataDto);
    }

    public void SendFlag(FlagDataDto flagData)
    {
        logger.LogInformation("[RaceControlHub] Received flag from client");
        categoryService.EnqueueFlag(flagData.Flag, flagData.Driver);
    }

    public async Task SessionFinalised()
    {
        logger.LogInformation("[RaceControlHub] Received stop current category message from client");
        await categoryService.StopActiveCategoryAsync();

    }
}