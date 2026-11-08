using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RaceControl.Database;
using RaceControl.Database.Entities;
using RaceControl.Server.Championships;

namespace RaceControl.Server.Jobs;

public class SyncSessionsJob(
    RaceControlContext raceControlContext,
    ILogger<SyncSessionsJob> logger,
    IEnumerable<IChampionship> championships) : IJob
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
            logger.LogInformation("[Session Sync] Championship calendar synchronization complete");
        }

        logger.LogInformation("[Session Sync] Session data synchronized");
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