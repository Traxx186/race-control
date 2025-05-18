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

        foreach (var category in categories)
        {
            // Fetch the calendar data of the current category, if no data is found go to the next
            // category.
            var calendar = await FetchCalendarAsync(category.Key, DateTime.UtcNow.Year);
            if (null == calendar)
            {
                logger.LogWarning("[Session Sync] Could not find session data for {key}", category.Key);
                continue;
            }

            var races = calendar.Races.SelectMany(r => r.Sessions.Select(s => new Session
            {
                CategoryKey = category.Key,
                Category = category,
                Name = r.Name,
                Key = s.Key,
                Time = s.Value
            }));

            logger.LogInformation("[Session Sync] Update database sessions for {key}", category.Key);
            foreach (var race in races)
            {
                // Query for a session of the given category, session name, session key and session year
                var existingSession = dbContext.Sessions
                    .SingleOrDefault(s => 
                        s.CategoryKey == category.Key && 
                        s.Key == race.Key && 
                        s.Name == race.Name &&
                        s.Time.Year == race.Time.Year);

                // If no session is found in the database, add the new session. Otherwise, the old session
                // will be updated with the session time.
                if (null == existingSession)
                    dbContext.Sessions.Add(race);
                else
                    existingSession.Time = race.Time;
            }
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
        Dictionary<string, DateTime> Sessions
    );
}