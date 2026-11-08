using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Database;
using RaceControl.Database.Entities;
using RaceControl.Server.Championships;

namespace RaceControl.Server.Jobs;

public class SyncSessionsJob(
    RaceControlContext raceControlContext,
    ILogger<SyncSessionsJob> logger,
    IEnumerable<IChampionship> championships,
    IMapper mapper,
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

        var championshipRecords = raceControlContext.Championships.ToArray();
        foreach (var championship in championshipRecords)
        {
            var championshipId = championship.Id;
            IChampionship? currentChampionship = championshipId switch
            {
                "f1" => championships.OfType<Formula1>().FirstOrDefault(),
                "f2" => championships.OfType<Formula2>().FirstOrDefault(),
                _ => null
            };

            if (currentChampionship is null)
                continue;

            logger.LogInformation("[Session Sync] Synchronized calendar for championship {key}", championshipId);
            var championshipCalendar = await currentChampionship.FetchRaceCalendarAsync();
            var events = championshipCalendar.Keys.Select(e => new Event
            {
                Championship = championship,
                Name = e.Name,
                MeetingKey = e.MeetingKey,
                Location = e.Location,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
            }).ToList();

            await UpsertEvents(events);

            // Create session entity objects from the DTO and link the correct event to the session.
            var sessions = championshipCalendar.Values
                .SelectMany(s => s)
                .Select(s => new Session
                {
                    Event = events.Single(e => e.MeetingKey == s.Event.MeetingKey),
                    Name = s.Name,
                    Key = s.Key,
                    Type = s.Type,
                    StartTime = s.StartTime

                })
                .ToList();

            await UpsertSessions(sessions);
        }

        logger.LogInformation("[Session Sync] Session data synchronized");
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

        raceControlContext.ChangeTracker.DetectChanges();
        await raceControlContext.SaveChangesAsync();
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

        raceControlContext.ChangeTracker.DetectChanges();
        await raceControlContext.SaveChangesAsync();
    }
}