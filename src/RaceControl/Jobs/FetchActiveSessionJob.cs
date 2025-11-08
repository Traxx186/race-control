using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;
using RaceControl.Database;
using RaceControl.Services;

namespace RaceControl.Jobs;

public class FetchActiveSessionJob(RaceControlContext dbContext, ILogger<SyncSessionsJob> logger, CategoryService categoryService, WebsocketService websocketService) : IJob
{
    public static readonly JobKey JobKey = new("FetchActiveSessionJob");
    
    public async Task Execute(IJobExecutionContext context)
    {
        if (categoryService.HasSessionActive)
            return;
        
        logger.LogInformation("[Fetch Session] Searching in database for active session");
        
        var signalTime = DateTime.Now.AddMinutes(5).ToUniversalTime();
        var searchDate = new LocalDateTime(signalTime.Year, signalTime.Month, signalTime.Day, signalTime.Hour, signalTime.Minute, 0);
        var session = dbContext.Sessions.Include(session => session.Category)
            .SingleOrDefault(s => s.Time == searchDate);
        
        // If no session has been found, stop the job.
        if (null == session)
            return;
        
        logger.LogInformation("[Fetch Session] Session found with key {key}, starting category service", session.CategoryKey);
        
        await websocketService.BroadcastEventAsync(MessageEvent.SessionChange, session.Category, CancellationToken.None);
        await categoryService.StartCategoryAsync(session);
    }
}