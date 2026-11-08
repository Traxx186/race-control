using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using RaceControl.Data.Dtos;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Enums;
using RaceControl.Data.Events;
using RaceControl.Server.SignalR;
using RaceControlMessageDto = RaceControl.Data.Dtos.LiveTimingDtos.RaceControlMessageDto;

namespace RaceControl.Server.Championships;

public class Formula2(
    ILogger<Formula2> logger,
    IHttpClientFactory httpClientFactory,
    CancellationTokenSource cts) : IChampionship
{
    private const string LiveTimingUrl = "https://ltss.fiaformula2.com";

    /// <summary>
    /// Which SignalR feeds to subscribe to when connection to the live timing API.
    /// </summary>
    private static readonly string[] Feeds = ["status"];

    /// <summary>
    /// The SignalR <see cref="Client"/> connection object.
    /// </summary>
    private Client? _signalR;

    /// <summary>
    /// If the session has actually started.
    /// </summary>
    private bool _hasStarted;

    /// <summary>
    /// The global CancellationToken.
    /// </summary>
    private readonly CancellationToken _cancellationToken = cts.Token;

    /// <inheritdoc/>
    public event EventHandler<FlagChangedEventArgs>? FlagParsed;

    /// <inheritdoc/>
    public event EventHandler? SessionFinished;

    /// <inheritdoc/>
    public bool Connected => _signalR?.Running ?? false;


    /// <inheritdoc/>
    public async Task StartAsync()
    {
        logger.LogInformation("[Formula 2] Starting API connection");

        _signalR = new Client(
            LiveTimingUrl,
            "streaming",
            new Version(2, 1),
            "/streaming"
        );

        _signalR.Error += _ => OnSessionFinishedAsync();
        _signalR.On<JsonArray>("trackfeed", HandleTrackFeedMessage);
        _signalR.On<JsonArray>("sessionfeed", HandleSessionFeedMessageAsync);

        await _signalR.StartAsync(_cancellationToken);
        await _signalR.InvokeAsync("JoinFeeds", _cancellationToken, "F2", Feeds);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        FlagParsed = null;

        if (!_hasStarted || _signalR is null)
            return;

        logger.LogInformation("[Formula 2] Closing API connection");
        _hasStarted = false;

        _signalR!.Stop();
        _signalR = null;
    }


    /// <inheritdoc/>
    /// Formula 2 live timing API doesn't support Race control messages
    public void ParseRaceControlMessage(RaceControlMessageDto raceControlMessage)
    {
        return;
    }

    /// <inheritdoc/>
    public void ParseTrackStatusMessage(TrackStatusMessageDto trackStatusMessage)
    {
        if (!short.TryParse(trackStatusMessage.Value, out var status))
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
    public void ParseSessionStatusMessageAsync(SessionStatusMessageDto sessionStatusMessage)
    {
        if (sessionStatusMessage.Value.Equals("Started", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 2] Session started");
            OnFlagParsed(Flag.Clear);
            _hasStarted = true;

            return;
        }

        if (sessionStatusMessage.Value.Equals("Finished", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 2] Session finished");
            OnFlagParsed(Flag.Chequered);

            return;
        }

        if (sessionStatusMessage.Value.Equals("Finalised", StringComparison.OrdinalIgnoreCase) && _hasStarted)
        {
            logger.LogInformation("[Formula 2] Session finalized, closing API connection");
            OnSessionFinishedAsync();

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
    private void OnSessionFinishedAsync()
    {
        Stop();

        SessionFinished?.Invoke(this, EventArgs.Empty);
        SessionFinished = null;
    }

    /// <summary>
    /// Deserialize the incoming Tack Feed message to get the current flag of the session.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 2 API.</param>
    private void HandleTrackFeedMessage(JsonArray message)
    {
        logger.LogInformation("[Formula 2] Parsing track feed message");

        var trackStatusMessage = message[1]?.Deserialize<TrackStatusMessageDto>();
        if (trackStatusMessage is null)
        {
            logger.LogError("[Formula 2] Invalid track status message received");
            return;
        }

        ParseTrackStatusMessage(trackStatusMessage);
    }

    /// <summary>
    /// Deserialize the incoming Session Feed message to check if the session is finished.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 2 API.</param>
    private void HandleSessionFeedMessageAsync(JsonArray message)
    {
        logger.LogInformation("[Formula 2] Parsing session feed message");
        var sessionStatusMessage = message[1]?.Deserialize<SessionStatusMessageDto>();
        if (sessionStatusMessage is null)
        {
            logger.LogError("[Formula 2] Invalid session feed message received");
            return;
        }

        ParseSessionStatusMessageAsync(sessionStatusMessage);
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