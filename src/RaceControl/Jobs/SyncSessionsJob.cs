using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Database;
using RaceControl.Database.Entities;

namespace RaceControl.Jobs;

public class SyncSessionsJob(RaceControlContext dbContext, ILogger<SyncSessionsJob> logger) : IJob
{
    private static readonly HttpClient HttpClient = new();
    
    public static readonly JobKey JobKey = new("SyncSessionsJob");

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("[Session Sync] Synchronizing session data with racing calendars");
        var categories = dbContext.Categories.ToArray();
        var currentYear = context.FireTimeUtc.Year;

        foreach (var category in categories)
        {
            // Fetch the calendar data of the current category, if no data is found go to the next
            // category.
            var calendar = await FetchCalendarAsync(category.Key, currentYear);
            if (null == calendar)
            {
                logger.LogWarning("[Session Sync] Could not find session data for {key}", category.Key);
                continue;
            }

            logger.LogInformation("[Session Sync] Check if sessions need to be removed due to cancellations {key}", category.Key);
            var cancelledRaces = calendar.Races
                .Where(r => r.Canceled)
                .ToArray();

            if (cancelledRaces.Length > 0)
            {
                logger.LogInformation("[Session Sync] Remove session of cancelled races");
                await DeleteSessions(category, currentYear, cancelledRaces);
            }
            
            var notCancelledRaces = calendar.Races
                .Where(r => !r.Canceled)
                .ToArray();

            logger.LogInformation("[Session Sync] Update database sessions for {key}", category.Key);
            UpsertSessions(category, currentYear, notCancelledRaces);
        }

        dbContext.ChangeTracker.DetectChanges();
        await dbContext.SaveChangesAsync();

        logger.LogInformation("[Session Sync] Session data synchronized");
    }

    /// <summary>
    /// Gets the sessions info for the given category
    /// </summary>
    /// <param name="category">The category to fetch the data for.</param>
    /// <param name="year">The year of the season.</param>
    /// <returns>The fetched data.</returns>
    private async Task<Calendar?> FetchCalendarAsync(string category, int year)
    {
        logger.LogInformation("[Session Sync] Fetching calendar data for {key}", category);

        var url = $"https://raw.githubusercontent.com/sportstimes/f1/main/_db/{category}/{year}.json";
        return await HttpClient.GetFromJsonAsync<Calendar>(url);
    }

    /// <summary>
    /// Inserts/update session in the database.
    /// </summary>
    /// <param name="category">The related category of the sessions.</param>
    /// <param name="year">The season year.</param>
    /// <param name="races">Races where the sessions added/updated.</param>
    private void UpsertSessions(Category category, int year, CalendarItem[] races)
    {
        var sessions = races.SelectMany(r => 
            r.Sessions.Select(s => new Session 
                {
                    Id = $"{category.Key}_{year}_{r.Round:00}_{s.Key}",
                    CategoryKey = category.Key,
                    Category = category,
                    Name = r.Name,
                    Key = s.Key,
                    Round = r.Round,
                    Time = s.Value
                }
            ))
            .ToArray();
        
        
        foreach (var session in sessions)
        {
            // Query for a session of the given category, session name, session key and session year
            var sessionId = $"{category.Key}_{year}_{session.Round:00}_{session.Key}";
            var existingSession = dbContext.Sessions
                .SingleOrDefault(s => s.Id == sessionId);

            // If no session is found in the database, add the new session. Otherwise, the old session
            // will be updated with the session time.
            if (null == existingSession)
                dbContext.Sessions.Add(session);
            else
                existingSession.Time = session.Time;
        }
    }

    /// <summary>
    /// Removes existing sessions from database.
    /// </summary>
    /// <param name="category">The related category of the sessions.</param>
    /// <param name="year">The season year.</param>
    /// <param name="races">Races where the sessions will be deleted.</param>
    private async Task DeleteSessions(Category category, int year, CalendarItem[] races)
    {
        var sessionKeys = races.SelectMany(r => 
                r.Sessions.Select(s => $"{category.Key}_{year}_{r.Round:00}_{s.Key}")
            )
            .ToArray();

        await dbContext.Sessions
            .Where(s => sessionKeys.Contains(s.Id))
            .ExecuteDeleteAsync();
    }
    
    /// <summary>
    /// The structure of the response from the calendar api.
    /// </summary>
    private record Calendar(
        CalendarItem[] Races
    );

    /// <summary>
    /// The structure of a calendar item of the calendar api.
    /// </summary>
    private record CalendarItem(
        string Name,
        int Round,
        bool Canceled,
        Dictionary<string, DateTime> Sessions
    );
}