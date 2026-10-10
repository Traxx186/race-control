using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Server.Services;

namespace RaceControl.Server.Hubs;

public class RaceControlHub(
    ILogger<RaceControlHub> logger,
    ITrackStatusService trackStatusService,
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

    public void SessionStatus(SessionStatusMessageDto sessionStatusMessage)
    {
        logger.LogInformation("[RaceControlHub] Received session status message from client");
        championshipService.ActiveChampionship?.ParseSessionStatusMessageAsync(sessionStatusMessage);
    }

    public void RaceControlMessage(RaceControlMessageDto raceControlMessage)
    {
        logger.LogInformation("[RaceControlHub] Received race control message from client");
        championshipService.ActiveChampionship?.ParseRaceControlMessage(raceControlMessage);
    }

    public void TrackStatusMessage(TrackStatusMessageDto trackStatusMessage)
    {
        logger.LogInformation("[RaceControlHub] Received track status message from client");
        championshipService.ActiveChampionship?.ParseTrackStatusMessage(trackStatusMessage);
    }
}