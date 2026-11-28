using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNet.SignalR.Client;
using RaceControl.Data.Dtos;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Enums;
using RaceControl.Data.Events;

namespace RaceControl.Server.Championships;

public class Formula2(
    ILogger<Formula2> logger,
    IHttpClientFactory httpClientFactory,
    CancellationTokenSource cts) : IChampionship
{
    private const string LiveTimingUrl = "https://ltss.fiaformula2.com/streaming";

    /// <summary>
    /// Which SignalR feeds to subscribe to when connection to the live timing API.
    /// </summary>
    private static readonly string[] Feeds = ["status"];

    /// <summary>
    /// The connection object.
    /// </summary>
    private HubConnection? _connection;

    /// <summary>
    /// Proxy of the connected hub.
    /// </summary>
    private IHubProxy? _proxy;

    /// <summary>
    /// If the session has actually started.
    /// </summary>
    private bool _hasStarted;

    /// <inheritdoc/>
    public event EventHandler<FlagChangedEventArgs>? FlagParsed;

    /// <inheritdoc/>
    public event EventHandler? SessionFinished;

    /// <inheritdoc/>
    public bool Connected => _connection!.State == ConnectionState.Connected;

    /// <inheritdoc/>
    public async Task StartAsync()
    {
        logger.LogInformation("[Formula 2] Starting API connection");

        _connection = new HubConnection(LiveTimingUrl, useDefaultUrl: false);
        _connection.CookieContainer = new CookieContainer();
        _connection.Protocol = new Version(2, 1);
        _connection.Closed += () => logger.LogInformation("[Formula 2] Hub connection closed");
        _connection.Reconnecting += () => logger.LogInformation("[Formula 2] Reconnecting");
        _connection.Reconnected += () => logger.LogInformation("[Formula 2] Reconnected");
        _connection.Error += e =>
        {
            logger.LogError("[Formula 2] Error occured: {error}", e.Message);
            OnSessionFinished();
        };

#if DEBUG
        _connection.TraceWriter = Console.Out;
        _connection.TraceLevel = TraceLevels.All;
#endif

        _proxy = _connection.CreateHubProxy("streaming");
        _proxy.On<string, JsonNode>("trackfeed", (time, data) => logger.LogInformation(data.ToJsonString()));
        _proxy.On<string, JsonNode>("sessionfeed", (time, data) => logger.LogInformation(data.ToJsonString()));

        await Task.WhenAny(_connection!.Start(), Task.Delay(Timeout.Infinite, cts.Token));
        await Task.WhenAny(_proxy!.Invoke("JoinFeeds", "F2", Feeds), Task.Delay(Timeout.Infinite, cts.Token));
    }

    /// <inheritdoc/>
    public void Stop()
    {
        FlagParsed = null;

        logger.LogInformation("[Formula 2] Closing API connection");
        _connection?.Stop();
        _proxy = null;
        _connection = null;
        _hasStarted = false;
    }


    /// <inheritdoc/>
    /// Formula 2 live timing API doesn't support Race control messages
    public void ParseRaceControlMessage(RaceControlMessageLiveTimingDto raceControlMessageLiveTiming)
    {
        return;
    }

    /// <inheritdoc/>
    public void ParseTrackStatusMessage(TrackStatusMessageLiveTimingDto trackStatusMessageLiveTiming)
    {
        if (!short.TryParse(trackStatusMessageLiveTiming.Value, out var status))
        {
            logger.LogError("[Formula 2] Invalid track status message received");
            return;
        }

        var flag = status switch
        {
            1 => Flag.Clear,
            2 => Flag.Yellow,
            4 => Flag.SafetyCar,
            5 => Flag.Red,
            6 => Flag.Vsc,
            _ => Flag.None
        };

        OnFlagParsed(flag);
    }

    /// <inheritdoc/>
    public void ParseSessionStatusMessageAsync(SessionStatusMessageLiveTimingDto sessionStatusMessageLiveTiming)
    {
        if (sessionStatusMessageLiveTiming.Value.Equals("Started", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 2] Session started");
            OnFlagParsed(Flag.Clear);
            _hasStarted = true;

            return;
        }

        if (sessionStatusMessageLiveTiming.Value.Equals("Finished", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 2] Session finished");
            OnFlagParsed(Flag.Chequered);

            return;
        }

        if (sessionStatusMessageLiveTiming.Value.Equals("Finalised", StringComparison.OrdinalIgnoreCase) && _hasStarted)
        {
            logger.LogInformation("[Formula 2] Session finalized, closing API connection");
            OnSessionFinished();

            return;
        }

        logger.LogInformation("[Formula 2] Session feed message ignored");
    }

    /// <inheritdoc/>
    public async Task<Dictionary<EventDto, SessionDto[]>> FetchRaceCalendarAsync()
    {
        var events = new Dictionary<EventDto, SessionDto[]>();
        var httpClient = httpClientFactory.CreateClient("Formula1Api");
        httpClient.DefaultRequestHeaders.Add("apiKey", Environment.GetEnvironmentVariable("APIKEY_F2API"));

        logger.LogInformation("[Formula 2] Fetch upcoming event");
        var data = await httpClient.GetFromJsonAsync<JsonObject>("/v1/core-event-tracker/f2");
        var eventInfo = data?["race"];
        if (eventInfo is null)
            return events;

        var @event = new EventDto
        {
            Name = eventInfo["meetingName"]!.GetValue<string>(),
            MeetingKey = data?["seasonContext"]?["currentOrNextMeetingKey"]?.GetValue<string>() ?? string.Empty,
            Location = eventInfo["meetingLocation"]!.GetValue<string>(),
            StartTime = DateTime.ParseExact(eventInfo["meetingStartDate"]!.GetValue<string>(),
                "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture).ToUniversalTime(),
            EndTime = DateTime.ParseExact(eventInfo["meetingEndDate"]!.GetValue<string>(), "yyyy-MM-ddTHH:mm:ss.fffZ",
                CultureInfo.InvariantCulture).ToUniversalTime()
        };

        // Get the sessions of the upcoming event.
        logger.LogInformation("[Formula 2] Get sessions for upcoming event {event}", @event.Name);
        var eventSessions = data?["race"]?["meetingSessions"]?.AsArray() ?? [];
        var sessions = eventSessions.Select(session => new SessionDto
            {
                Event = @event,
                Name = session!["shortName"]!.GetValue<string>(),
                Key = session["meetingSessionKey"]!.GetValue<int>(),
                Type = session["sessionType"]!.GetValue<string>(),
                StartTime = ParseDateTime(session["startTime"]!.GetValue<string>(), session["gmtOffset"]!.GetValue<string>())
            })
            .ToArray();

        events[@event] = sessions;
        return events;
    }

    /// <summary>
    /// Invokes the FlagPares event with the required arguments
    /// </summary>
    /// <param name="flag">The parsed flag.</param>
    private void OnFlagParsed(Flag flag)
    {
        var args = new FlagChangedEventArgs { Flag = flag };

        FlagParsed?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes the SessionFinished event.
    /// </summary>
    private void OnSessionFinished()
    {
        Stop();

        SessionFinished?.Invoke(this, EventArgs.Empty);
        SessionFinished = null;
    }

    /// <summary>
    /// Parses the timestamp plus offset to a UTC <see cref="DateTime"/>.
    /// </summary>
    /// <param name="timestamp">Timestamp to parse.</param>
    /// <param name="offset">Offset of the given timestamp.</param>
    /// <returns>The parsed <see cref="DateTime"/>.</returns>
    private static DateTime ParseDateTime(string timestamp, string offset) =>
        DateTime.ParseExact($"{timestamp}{offset}", "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)
            .ToUniversalTime();
}