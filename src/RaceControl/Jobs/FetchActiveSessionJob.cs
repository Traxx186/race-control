using Quartz;
using RaceControl.Database;

namespace RaceControl.Jobs;

public class FetchActiveSessionJob(RaceControlContext dbContext, ILogger<SyncSessionsJob> logger, CategoryService categoryService) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        if (categoryService.HasSessionActive)
            return Task.CompletedTask;
        
        logger.LogInformation("[Fetch Session] Searching in database for active session");
        
        var signalTime = DateTime.Now.AddMinutes(5).ToUniversalTime();
        var searchDate = new DateTime(signalTime.Year, signalTime.Month, signalTime.Day, signalTime.Hour, signalTime.Minute, 0);
        var session = dbContext.Sessions
            .SingleOrDefault(s => s.Time == searchDate);
        
        // If no session has been found, stop the job.
        if (null == session)
            return Task.CompletedTask;

        logger.LogInformation("[Fetch Session] Session found with key {key}, starting category service", session.CategoryKey);
        categoryService.StartCategory(session);
        
        return Task.CompletedTask;
    }
}