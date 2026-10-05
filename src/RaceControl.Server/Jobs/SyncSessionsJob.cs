using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Quartz;
using RaceControl.Data.Dtos;
using RaceControl.Database;
using RaceControl.Database.Entities;

namespace RaceControl.Server.Jobs;

public class SyncSessionsJob(
    RaceControlContext dbContext,
    ILogger<SyncSessionsJob> logger,
    IHttpClientFactory httpClientFactory
    ) : IJob
{
    /// <summary>
    /// JSON Serialization options relevant for the F1 calendar API.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Session Sync] Synchronizing session data with racing calendars");

        var championships = dbContext.Championships.ToArray();
        foreach (var championship in championships)
        {
            var sessions = championship.Id switch
            {
                "f1" => await FetchF1SessionsAsync(),
                _ => []
            };

            logger.LogInformation("[Session Sync] Update database sessions for {key}", championship.Id);

            sessions.ForEach(session => session.Championship = championship);
            UpsertSessions(championship, sessions);
        }

        dbContext.ChangeTracker.DetectChanges();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Session Sync] Session data synchronized");
    }

    /// <summary>
    /// Gets all the session in the current Formula 1 season using the official Formula 1 API.
    /// </summary>
    /// <returns>List of <see cref="Session"/> in the current season.</returns>
    private async Task<List<Session>> FetchF1SessionsAsync()
    {
        var sessions = new List<Session>();
        var httpClient = httpClientFactory.CreateClient("Formula1Api");
        httpClient.DefaultRequestHeaders.Add("apiKey", Environment.GetEnvironmentVariable("APIKEY_F1API"));

        logger.LogInformation("[Session Sync] Fetch Formula 1 events");
        var data = await httpClient.GetFromJsonAsync<JsonObject>("/v1/editorial-eventlisting/events");
        var events = data?["events"].Deserialize<EventDto[]>(JsonOptions) ?? [];

        logger.LogInformation("[Session Sync] Fetch sessions for each Formula 1 event");
        foreach (var e in events)
        {
            var eventData = await httpClient.GetFromJsonAsync<JsonObject>($"/v1/event-tracker/meeting/{e.MeetingKey}");
            var eventSessions = eventData?["meetingContext"]?["timetables"].Deserialize<SessionDto[]>(JsonOptions) ?? [];

            sessions.AddRange(eventSessions.Select(session => new Session
            {
                Event = e.MeetingName,
                Name = session.Description,
                Key = session.MeetingSessionKey,
                Type = session.SessionType,
                StartTime = DateTime.ParseExact($"{session.StartTime}{session.GmtOffset}", "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture).ToUniversalTime()
            }));
        }

        return sessions;
    }

    /// <summary>
    /// Inserts/update session in the database.
    /// </summary>
    /// <param name="championship">The related championship of the sessions.</param>
    /// <param name="sessions">The sessions to update/insert.</param>
    private void UpsertSessions(Championship championship, List<Session> sessions)
    {
        foreach (var session in sessions)
        {
            var existingSession = dbContext.Sessions
                .SingleOrDefault(s => s.Key == session.Key && s.ChampionshipId == championship.Id);

            // If no session is found in the database, add the new session. Otherwise, the old session
            // will be updated with the session time.
            if (null == existingSession)
                dbContext.Sessions.Add(session);
            else
                existingSession.StartTime = session.StartTime;
        }
    }
}