using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Data.Dtos.Signalr;
using RaceControl.Database;
using RaceControl.Server.Hubs;
using RaceControl.Server.Services;

namespace RaceControl.Server.Jobs;

public class FetchActiveSessionJob(
    ILogger<SyncSessionsJob> logger,
    IHubContext<RaceControlHub, IRaceControlHubClient> raceControlHubContext,
    RaceControlContext raceControlContext,
    IChampionshipService championshipService) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (championshipService.HasSessionActive)
            return;

        logger.LogInformation("[Fetch Session] Searching in database for active session");

        var signalTime = DateTime.UtcNow.AddMinutes(5);
        var searchDate = new DateTime(signalTime.Year, signalTime.Month, signalTime.Day, signalTime.Hour, signalTime.Minute, 0, DateTimeKind.Utc);
        var session = raceControlContext.Sessions.Include(session => session.Event.Championship)
            .SingleOrDefault(s => s.StartTime == searchDate);

        // If no session has been found, stop the job.
        if (null == session)
            return;

        var championshipId = session.Event.ChampionshipId;
        logger.LogInformation("[Fetch Session] Session found with key {key}, starting category service", championshipId);

        var championshipDto = new ChampionshipSignalrDto
        {
            Id = championshipId,
            Name = session.Event.Championship.Name,
        };

        await raceControlHubContext.Clients.All.ChampionshipChange(championshipDto);
        await championshipService.StartCategoryAsync(session);
    }
}