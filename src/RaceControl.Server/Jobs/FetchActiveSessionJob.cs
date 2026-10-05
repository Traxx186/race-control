using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Data.Dtos;
using RaceControl.Database;
using RaceControl.Server.Hubs;
using RaceControl.Server.Services;

namespace RaceControl.Server.Jobs;

public class FetchActiveSessionJob(
    ILogger<SyncSessionsJob> logger,
    IHubContext<RaceControlHub, IRaceControlHubClient> racHubContext,
    RaceControlContext dbContext,
    ICategoryService categoryService) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (categoryService.HasSessionActive)
            return;

        logger.LogInformation("[Fetch Session] Searching in database for active session");

        var signalTime = DateTime.Now.AddMinutes(5).ToUniversalTime();
        var searchDate = new DateTime(signalTime.Year, signalTime.Month, signalTime.Day, signalTime.Hour, signalTime.Minute, 0, DateTimeKind.Utc);
        var session = dbContext.Sessions.Include(session => session.Championship)
            .SingleOrDefault(s => s.StartTime == searchDate);

        // If no session has been found, stop the job.
        if (null == session)
            return;

        logger.LogInformation("[Fetch Session] Session found with key {key}, starting category service", session.ChampionshipId);

        var category = new CategoryDto(Latency: 35, Key: session.ChampionshipId);
        await racHubContext.Clients.All.CategoryChange(category);
        await categoryService.StartCategoryAsync(session);
    }
}