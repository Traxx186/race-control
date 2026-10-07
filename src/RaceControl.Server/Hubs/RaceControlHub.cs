using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Server.Services;

namespace RaceControl.Server.Hubs;

public class RaceControlHub(
    ITrackStatusService trackStatusService,
    ILogger<RaceControlHub> logger,
    IChampionshipService championshipService) : Hub<IRaceControlHubClient>
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[RaceControlHub] New client connected, send flag data & current category if present");
        var championship = championshipService.ActiveSession?.Championship;
        var flagDataDto = new FlagDataDto(trackStatusService.ActiveFlag);

        if (championship != null)
        {
            var categoryDto = new CategoryDto(championship.Id, 35);
            await Clients.Caller.CategoryChange(categoryDto);
            await Task.Delay(35_000);
        }

        await Clients.Caller.FlagChange(flagDataDto);
    }

    public void SendFlag(FlagDataDto flagData)
    {
        logger.LogInformation("[RaceControlHub] Received flag from client");
        championshipService.EnqueueFlag(flagData);
    }

    public async Task SessionFinalised()
    {
        logger.LogInformation("[RaceControlHub] Received stop current category message from client");
        await championshipService.StopActiveCategoryAsync();
    }
}