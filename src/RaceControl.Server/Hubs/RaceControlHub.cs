using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Dtos.Signalr;
using RaceControl.Database;
using RaceControl.Database.Entities;
using RaceControl.Server.Services;

namespace RaceControl.Server.Hubs;

public class RaceControlHub(
    ILogger<RaceControlHub> logger,
    IMapper mapper,
    ITrackStatusService trackStatusService,
    IChampionshipService championshipService) : Hub<IRaceControlHubClient>
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[RaceControlHub] New client connected, send flag data & current category if present");
        var championship = championshipService.ActiveSession?.Event.Championship;
        var flagDataDto = new FlagDataSignalrDto { Flag = trackStatusService.ActiveFlag };

        if (championship != null)
        {
            var championShip = new ChampionshipSignalrDto { Id = championship.Id, Name = championship.Name };
            await Clients.Caller.ChampionshipChange(championShip);
            await Task.Delay(championship.Latency);
        }

        await Clients.Caller.FlagChange(flagDataDto);
    }

    public async Task SendSessionStatus(SessionStatusMessageLiveTimingDto sessionStatusMessageLiveTiming, RaceControlContext raceControlContext)
    {
        logger.LogInformation("[RaceControlHub] Received session status message from client");
        championshipService.ActiveChampionship?.ParseSessionStatusMessageAsync(sessionStatusMessageLiveTiming);

        if (championshipService.ActiveSession is null)
            return;

        logger.LogInformation("[RaceControlHub] Store session status message in database");
        var state = sessionStatusMessageLiveTiming.Status ?? sessionStatusMessageLiveTiming.Value;
        var message = new RaceControlMessage
        {
            SessionId = championshipService.ActiveSession.Id,
            Date = DateTime.UtcNow,
            Category = MessageCategory.SessionStatus,
            Message = $"SESSION {state?.ToUpper()}"
        };

        raceControlContext.RacesControlMessages.Add(message);
        await raceControlContext.SaveChangesAsync();
    }

    public async Task SendRaceControlMessage(RaceControlMessageLiveTimingDto raceControlMessage, RaceControlContext raceControlContext)
    {
        logger.LogInformation("[RaceControlHub] Received race control message from client");
        championshipService.ActiveChampionship?.ParseRaceControlMessage(raceControlMessage);

        if (championshipService.ActiveSession is null)
            return;

        logger.LogInformation("[RaceControlHub] Store race control message in database");
        var message = mapper.Map<RaceControlMessageLiveTimingDto, RaceControlMessage>(raceControlMessage);
        raceControlContext.RacesControlMessages.Add(message);
        await raceControlContext.SaveChangesAsync();
    }

    public void SendTrackStatusMessage(TrackStatusMessageLiveTimingDto trackStatusMessageLiveTiming)
    {
        logger.LogInformation("[RaceControlHub] Received track status message from client");
        championshipService.ActiveChampionship?.ParseTrackStatusMessage(trackStatusMessageLiveTiming);
    }
}