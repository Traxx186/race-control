using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Dtos.Signalr;
using RaceControl.Database;
using RaceControl.Database.Entities;
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

    public async Task SendSessionStatus(SessionStatusMessageDto sessionStatusMessage, RaceControlContext raceControlContext)
    {
        logger.LogInformation("[RaceControlHub] Received session status message from client");
        championshipService.ActiveChampionship?.ParseSessionStatusMessageAsync(sessionStatusMessage);

        if (championshipService.ActiveSession is null)
            return;

        logger.LogInformation("[RaceControlHub] Store session status message in database");
        var state = sessionStatusMessage.Status ?? sessionStatusMessage.Value;
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

    public async Task SendRaceControlMessage(RaceControlMessageDto raceControlMessage, RaceControlContext raceControlContext)
    {
        logger.LogInformation("[RaceControlHub] Received race control message from client");
        championshipService.ActiveChampionship?.ParseRaceControlMessage(raceControlMessage);

        if (championshipService.ActiveSession is null)
            return;

        logger.LogInformation("[RaceControlHub] Store race control message in database");
        var driverNumber = !string.IsNullOrWhiteSpace(raceControlMessage.RacingNumber)
            ? int.Parse(raceControlMessage.RacingNumber)
            : (int?)null;

        var scope = !string.IsNullOrWhiteSpace(raceControlMessage.Scope)
            ? RaceControlMessage.ParseScope(raceControlMessage.Scope)
            : (MessageScope?)null;

        var message = new RaceControlMessage
        {
            SessionId = championshipService.ActiveSession.Id,
            Date = raceControlMessage.Utc.ToUniversalTime(),
            Category = RaceControlMessage.ParseCategory(raceControlMessage.Category),
            Message = raceControlMessage.Message,
            Flag = raceControlMessage.Flag,
            Scope = scope,
            DriverNumber = driverNumber,
            Lap = raceControlMessage.Lap,
            Sector = raceControlMessage.Sector,
        };

        raceControlContext.RacesControlMessages.Add(message);
        await raceControlContext.SaveChangesAsync();
    }

    public void SendTrackStatusMessage(TrackStatusMessageDto trackStatusMessage)
    {
        logger.LogInformation("[RaceControlHub] Received track status message from client");
        championshipService.ActiveChampionship?.ParseTrackStatusMessage(trackStatusMessage);
    }
}