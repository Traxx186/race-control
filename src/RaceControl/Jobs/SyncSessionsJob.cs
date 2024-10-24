using Quartz;
using RaceControl.Database;
using RaceControl.Database.Entities;

namespace RaceControl.Jobs;

public class SyncSessionsJob(RaceControlContext _dbContext, ILogger<SyncSessionsJob> _logger) : IJob
{
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("[Session Sync] Synchronizing session data with racing calendars");
        var categories = _dbContext.Categories.ToArray();
        foreach (var category in categories)
        {
            // Fetch the calendar data of the current category, if no data is found go to the next
            // category.
            var calendar = await FetchCalendar(category.Key, DateTime.UtcNow.Year);
            if (null == calendar)
            {
                _logger.LogWarning("[Session Sync] Could not find session data for {key}", category.Key);
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
            
            _logger.LogInformation("[Session Sync] Update database sessions for {key}", category.Key);
            foreach (var race in races)
            {
                // Query for a session with the given category, time and name
                var existingSession = _dbContext.Sessions
                    .SingleOrDefault(s => s.CategoryKey == category.Key && s.Time == race.Time && s.Name == race.Name);

                // If no session is found in the database, add the new session. Otherwise the old session
                // will be updated with the session time.
                if (null == existingSession)
                    _dbContext.Sessions.Add(race);
                else
                    existingSession.Time = race.Time;
            }

            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("[Session Sync] Session data synchronized");
    }

    /// <summary>
    /// Gets the sessions info for the given category
    /// </summary>
    /// <param name="category">The category to fetch the data for.</param>
    /// <returns>The fetched data.</returns>
    private async Task<Calendar?> FetchCalendar(string category, int year)
    {
        _logger.LogInformation("[Session Sync] Fetching calendar data for {key}", category);
        
        var url = $"https://raw.githubusercontent.com/sportstimes/f1/main/_db/{category}/{year}.json";
        return await _httpClient.GetFromJsonAsync<Calendar>(url);
    }

    /// <summary>
    /// The structure of the response from the calendar api.
    /// </summary>
    private record class Calendar(
        CalendarItem[] Races
    );

    /// <summary>
    /// The structure of a calendar item of the calendar api.
    /// </summary>
    private record class CalendarItem
    (
        /// <summary>
        /// The name of the event.
        /// </summary>
        string Name,
        /// <summary>
        /// The session data of the event.
        /// </summary>
        Dictionary<string, DateTime> Sessions
    );
}