using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Data.Dtos;
using RaceControl.Database;
using RaceControl.Hubs;
using RaceControl.Services;

namespace RaceControl.Jobs;

public class FetchActiveSessionJob(
    ILogger<SyncSessionsJob> logger,
    IHubContext<RaceControlHub, IRaceControlHubClient> racHubContext,
    RaceControlContext dbContext,
    ICategoryService categoryService) : IJob
{
    public static readonly JobKey JobKey = new("FetchActiveSessionJob");

    public async Task Execute(IJobExecutionContext context)
    {
        if (categoryService.HasSessionActive)
            return;

        logger.LogInformation("[Fetch Session] Searching in database for active session");

        var signalTime = DateTime.Now.AddMinutes(5).ToUniversalTime();
        var searchDate = new DateTime(signalTime.Year, signalTime.Month, signalTime.Day, signalTime.Hour, signalTime.Minute, 0, DateTimeKind.Utc);
        var session = dbContext.Sessions.Include(session => session.Category)
            .SingleOrDefault(s => s.Time == searchDate);

        // If no session has been found, stop the job.
        if (null == session)
            return;

        logger.LogInformation("[Fetch Session] Session found with key {key}, starting category service", session.CategoryKey);

        var category = new CategoryDto(Latency: session.Category.Latency, Key: session.CategoryKey);
        await racHubContext.Clients.All.CategoryChange(category);
        await categoryService.StartCategoryAsync(session);
    }
}