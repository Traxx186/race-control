using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Data.Dtos;
using RaceControl.Database;
using RaceControl.Database.Entities;

namespace RaceControl.Server.Jobs;

public class SyncSessionsJob(
    RaceControlContext raceControlContext,
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

        var championships = raceControlContext.Championships.ToArray();
        foreach (var championship in championships)
        {
            switch(championship.Id)
            {
                case "f1":
                    await FetchF1SessionsAsync(championship);
                    break;
                case "f2":
                    await FetchUpcomingF2EventSessionsAsync(championship);
                    break;
                default:
                    logger.LogWarning("[Session Sync] No sync script for championship {championship}",  championship.Id);
                    break;
            }
        }

        logger.LogInformation("[Session Sync] Session data synchronized");
    }

    /// <summary>
    /// Gets all the session in the current Formula 1 season using the official Formula 1 API.
    /// </summary>
    private async Task FetchF1SessionsAsync(Championship championship)
    {
        var sessions = new List<Session>();
        var httpClient = httpClientFactory.CreateClient("Formula1Api");
        httpClient.DefaultRequestHeaders.Add("apiKey", Environment.GetEnvironmentVariable("APIKEY_F1API"));

        logger.LogInformation("[Session Sync] Fetch Formula 1 events");
        var data = await httpClient.GetFromJsonAsync<JsonObject>("/v1/editorial-eventlisting/events");
        var listedEvents = data?["events"]?.AsArray() ?? [];
        var events = listedEvents.Select(e => new Event
            {
                Championship = championship,
                Name = e!["meetingName"]!.GetValue<string>(),
                MeetingKey = e["meetingKey"]!.GetValue<string>(),
                Location = e["meetingLocation"]!.GetValue<string>(),
                StartTime = DateTime.ParseExact($"{e["meetingStartDate"]!.GetValue<string>()}{e["gmtOffset"]!.GetValue<string>()}", "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture).ToUniversalTime(),
                EndTime = DateTime.ParseExact($"{e["meetingEndDate"]!.GetValue<string>()}{e["gmtOffset"]!.GetValue<string>()}", "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture).ToUniversalTime()
            })
            .ToList();

        await UpsertEvents(events);
        raceControlContext.ChangeTracker.DetectChanges();
        await raceControlContext.SaveChangesAsync();

        logger.LogInformation("[Session Sync] Fetch sessions for each Formula 1 event");
        foreach (var e in events)
        {
            var eventData = await httpClient.GetFromJsonAsync<JsonObject>($"/v1/event-tracker/meeting/{e.MeetingKey}");
            var eventSessions = eventData?["meetingContext"]?["timetables"]?.AsArray() ?? [];

            sessions.AddRange(eventSessions.Select(session => new Session
            {
                Event = e,
                Name = session!["description"]!.GetValue<string>(),
                Key = session["meetingSessionKey"]!.GetValue<int>(),
                Type = session["sessionType"]!.GetValue<string>(),
                StartTime = DateTime.ParseExact($"{session["startTime"]}{session["gmtOffset"]}", "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture).ToUniversalTime()
            }));
        }

        await UpsertSessions(sessions);
        raceControlContext.ChangeTracker.DetectChanges();
        await raceControlContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the upcoming Formula 2 event sessions from the official Formula 1 API.
    /// </summary>
    private async Task FetchUpcomingF2EventSessionsAsync(Championship championship)
    {
        var httpClient = httpClientFactory.CreateClient("Formula1Api");
        httpClient.DefaultRequestHeaders.Add("apiKey", Environment.GetEnvironmentVariable("APIKEY_F2API"));

        logger.LogInformation("[Session Sync] Fetch upcoming Formula 2 event");
        var data = await httpClient.GetFromJsonAsync<JsonObject>("/v1/core-event-tracker/f2");
        var eventInfo = data?["race"];
        if (eventInfo is null)
        {
            logger.LogInformation("[Session Sync] No upcoming Formula 2 event, skip sync");
            return;
        }

        logger.LogInformation("[Session Sync] Found upcoming Formula 2 event, sync event data");
        var @event = new Event
        {
            Championship = championship,
            Name = eventInfo["meetingName"]!.GetValue<string>(),
            MeetingKey = data?["seasonContext"]?["currentOrNextMeetingKey"]?.GetValue<string>() ?? string.Empty,
            Location = eventInfo["meetingLocation"]!.GetValue<string>(),
            StartTime = DateTime.ParseExact(eventInfo["meetingStartDate"]!.GetValue<string>(), "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture).ToUniversalTime(),
            EndTime = DateTime.ParseExact(eventInfo["meetingEndDate"]!.GetValue<string>(), "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture).ToUniversalTime()
        };

        // Add/update upcoming event in the database.
        await UpsertEvents([@event]);
        raceControlContext.ChangeTracker.DetectChanges();
        await raceControlContext.SaveChangesAsync();

        logger.LogInformation("[Session Sync] Sync sessions for Formula 2 event");
        var eventSessions = data?["race"]?["meetingSessions"]?.AsArray() ?? [];
        var sessions = eventSessions.Select(session => new Session
            {
                Event = @event,
                Name = session!["shortName"]!.GetValue<string>(),
                Key = session["meetingSessionKey"]!.GetValue<int>(),
                Type = session["sessionType"]!.GetValue<string>(),
                StartTime = DateTime.ParseExact($"{session["startTime"]}{session["gmtOffset"]}", "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture).ToUniversalTime()
            })
            .ToList();

        // Add/update sessions of the upcoming event in the database.
        await UpsertSessions(sessions);
        raceControlContext.ChangeTracker.DetectChanges();
        await raceControlContext.SaveChangesAsync();
    }

    /// <summary>
    /// Inserts/update events in the database.
    /// </summary>
    /// <param name="events">The events to update/insert.</param>
    private async Task UpsertEvents(List<Event> events)
    {
        foreach (var @event in events)
        {
            // Try to update an existing event.
            var affectedRows = await raceControlContext.Events
                .Where(e => e.MeetingKey == @event.MeetingKey && e.ChampionshipId == @event.Championship.Id)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(e => e.Name, @event.Name)
                    .SetProperty(e => e.Location, @event.Location)
                    .SetProperty(e => e.StartTime, @event.StartTime)
                    .SetProperty(e => e.EndTime, @event.EndTime));

            // If no event could be updated, add the new event to the database.
            if (affectedRows == 0)
                raceControlContext.Events.Add(@event);
        }
    }

    /// <summary>
    /// Inserts/update session in the database.
    /// </summary>
    /// <param name="sessions">The sessions to update/insert.</param>
    private async Task UpsertSessions(List<Session> sessions)
    {
        foreach (var session in sessions)
        {
            session.Event = raceControlContext.Events
                .Single(e => e.MeetingKey == session.Event.MeetingKey && e.ChampionshipId == session.Event.Championship.Id);

            // Try to update an existing session.
            var affectedRows = await raceControlContext.Sessions
                .Where(s => s.Key == session.Key && s.EventId == session.Event.Id)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(s => s.Name, session.Name)
                    .SetProperty(s => s.StartTime, session.StartTime));

            // If no session could be updated, add the new session to the database.
            if (affectedRows == 0)
                raceControlContext.Sessions.Add(session);
        }
    }
}