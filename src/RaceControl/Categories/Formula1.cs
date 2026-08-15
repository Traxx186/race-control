using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Enums;
using RaceControl.Data.Events;
using RaceControl.Options;
using RaceControl.Services;

namespace RaceControl.Categories;

public sealed class Formula1: ICategory
{
    private const string LiveTimingUrl = "wss://livetiming.formula1.com/signalrcore";

    private readonly ILogger _logger;
    private readonly IOptionsMonitor<RaceControlOptions> _optionsMonitor;
    private readonly IF1AuthService _f1AuthService;

    /// <summary>
    /// Which SignalR topics to subscribe to when connection to the live timing API.
    /// </summary>
    private static readonly string[] Topics = ["TrackStatus", "RaceControlMessages", "SessionStatus"];

    /// <summary>
    /// The SignalR <see cref="HubConnection"/> connection object.
    /// </summary>
    private HubConnection? _connection;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler<FlagChangedEventArgs>? FlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler? SessionFinished;

    /// <inheritdoc/>
    public bool Connected => _connection?.State == HubConnectionState.Connected;

    public Formula1(
        ILogger<Formula1> logger,
        IOptionsMonitor<RaceControlOptions> options,
        IF1AuthService f1AuthService)
    {
        _logger = logger;
        _optionsMonitor = options;
        _f1AuthService = f1AuthService;

        _optionsMonitor.OnChange(async _ =>
        {
            if (_connection is null)
                return;

            _logger.LogInformation("[Formula 1] Config changed, restart Live timing");
            await StartAsync(string.Empty);
        });
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StartAsync(string session)
    {
        _logger.LogInformation("[Formula 1] Starting Live Timing connection");

        if (_connection is not null)
        {
            _logger.LogWarning("[Formula 1] Connection already active, restarting");
            await DisposeConnection();
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(LiveTimingUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_f1AuthService.AccessToken);
            })
            .ConfigureLogging(logging => logging.AddConsole())
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += async _ =>
        {
            _logger.LogInformation("[Formula 1] API connection terminated");
            await OnSessionFinished();
        };

        _connection.On<string, JsonNode, DateTimeOffset>("feed", HandleMessageAsync);
        await _connection.StartAsync();

        _logger.LogInformation("[Formula 1] Subscribe to selected topics");
        await _connection.InvokeAsync("Subscribe", Topics);

        _logger.LogInformation("[Formula 1] Live Timing connected");
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("[Formula 1] Closing API connection");
        await DisposeConnection();

        FlagParsed = null;
    }

    /// <summary>
    /// Closes the SignalR connection.
    /// </summary>
    private async Task DisposeConnection()
    {
        if (_connection is not null)
            await _connection!.StopAsync();

        _connection = null;
    }

    /// <summary>
    /// Invokes the FlagPares event with the required arguments
    /// </summary>
    /// <param name="flag">The parsed flag.</param>
    /// <param name="driverNumber">The number of the driver for whom the flag is intended.</param>
    private void OnFlagParsed(Flag flag, int? driverNumber = null)
    {
        var args = new FlagChangedEventArgs { Flag = flag, Driver = driverNumber};
        FlagParsed?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes the SessionFinished event.
    /// </summary>
    private async Task OnSessionFinished()
    {
        if (_connection?.State == HubConnectionState.Connected)
            await StopAsync();

        SessionFinished?.Invoke(this, EventArgs.Empty);
        SessionFinished = null;
    }

    /// <summary>
    /// Checks which function needs to be called based on the given topic.
    /// </summary>
    /// <param name="topic">Topic of the incoming message.</param>
    /// <param name="data">Date of the incoming message.</param>
    /// <param name="timestamp">When the incoming message was sent.</param>
    private async Task HandleMessageAsync(string topic, JsonNode data, DateTimeOffset timestamp)
    {
        switch (topic)
        {
            case "SessionStatus":
                await HandleSessionStatusMessageAsync(data);
                break;
            case "TrackStatus":
                HandleTrackStatusMessage(data);
                break;
            case "RaceControlMessages":
                HandleRaceControlMessages(data);
                break;
            default:
                _logger.LogWarning("[Formula 1] Unsupported topic {topic}", topic);
                break;
        }
    }

    /// <summary>
    /// Parses a track status message to a flag and relative data.
    /// </summary>
    /// <param name="data">Message object.</param>
    /// <returns>Parsed flag.</returns>
    private void HandleTrackStatusMessage(JsonNode data)
    {
        _logger.LogInformation("[Formula 1] Parsing track status message");
        var trackStatusMessage = data.Deserialize<TrackStatusMessageDto>();
        if (!short.TryParse(trackStatusMessage?.Status, out var status))
        {
            _logger.LogError("[Formula 1] Invalid track status message received");
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

    /// <summary>
    /// Parses a race control message to a flag and relative data.
    /// </summary>
    /// <param name="data">Race control message data.</param>
    private void HandleRaceControlMessages(JsonNode data)
    {
        _logger.LogInformation("[Formula 1] Parsing race control message");

        var raceControlMessages = data.Deserialize<RaceControlMessagesDto>();
        var raceControlMessage = raceControlMessages?.Messages[0].Deserialize<RaceControlMessageDto>();
        if (raceControlMessage is null)
        {
            _logger.LogWarning("[Formula 1] Invalid race control message received");
            return;
        }

        // Checks if the slippery surface flag is shown.
        if (raceControlMessage.Message.Contains("slippery", StringComparison.CurrentCultureIgnoreCase))
        {
            _logger.LogInformation("[Formula 1] Parsed race control message to {flag}", Flag.Surface);
            OnFlagParsed(Flag.Surface);

            return;
        }

        // If the message category is not 'Flag', or received clear message, the message can be ignored.
        if (raceControlMessage is not { Category: "Flag" } or { Flag: "CLEAR" })
        {
            _logger.LogInformation("[Formula 1] Race control message ignored");
            return;
        }

        // Checks if the flag message contains a valid flag and if the flag should be ignored.
        if (!TrackStatusService.TryParseFlag(raceControlMessage.Flag, out var flag))
        {
            _logger.LogWarning("[Formula 1] Could not parse flag '{flag}'", raceControlMessage.Flag);
            return;
        }

        if (!int.TryParse(raceControlMessage.RacingNumber, out var driver))
            driver = 0;

        OnFlagParsed(flag, driver == 0 ? null : driver);
    }

    /// <summary>
    /// Parses a session status message. If the message equals a finalized message, the session finished event will
    /// be triggered.
    /// </summary>
    /// <param name="data">Session status message</param>
    private async Task HandleSessionStatusMessageAsync(JsonNode data)
    {
        _logger.LogInformation("[Formula 1] Parsing session status message");

        var message = data.Deserialize<SessionStatusMessageDto>();
        if (message is null)
        {
            _logger.LogWarning("[Formula 1] Invalid session status message received");
            return;
        }

        if (!message.Status.Equals("finalised", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[Formula 1] Session status message ignored");
            return;
        }

        _logger.LogInformation("[Formula 1] Session finalised, stopping live timing");
        await OnSessionFinished();
    }
}