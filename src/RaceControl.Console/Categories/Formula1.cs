using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RaceControl.Console.Events;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Enums;

namespace RaceControl.Console.Categories;

public sealed class Formula1 : IChampionship
{
    private const string LiveTimingUrl = "https://livetiming.formula1.com/signalrcore";

    private readonly ILogger _logger;

    /// <summary>
    /// Which SignalR topics to subscribe to when connection to the live timing API.
    /// </summary>
    private static readonly string[] Topics = ["TrackStatus", "RaceControlMessages", "SessionStatus"];

    /// <summary>
    /// The SignalR <see cref="HubConnection"/> connection object.
    /// </summary>
    private readonly HubConnection _connection;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler<MessageEventArgs>? MessageReceived;

    public Formula1(ILogger<Formula1> logger)
    {
        _logger = logger;

        _connection = new HubConnectionBuilder()
            .WithUrl(LiveTimingUrl)
            .ConfigureLogging(logging => logging.AddConsole())
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += _ =>
        {
            _logger.LogInformation("[Formula 1] API connection terminated");
            return Task.CompletedTask;
        };

        _connection.On<string, JsonNode, DateTimeOffset>("feed", HandleMessageAsync);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StartAsync()
    {
        _logger.LogInformation("[Formula 1] Starting Live Timing connection");

        if (_connection.State == HubConnectionState.Connected)
        {
            _logger.LogWarning("[Formula 1] Connection already active, restarting");
            await StopAsync();
            await Task.Delay(1000);
        }

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

        if (_connection.State == HubConnectionState.Connected)
            await _connection!.StopAsync();
    }

    /// <summary>
    /// Invokes the SessionFinished event.
    /// </summary>
    private async Task OnSessionFinished()
    {
        if (_connection.State == HubConnectionState.Connected)
            await StopAsync();

        // clear event handlers
        MessageReceived = null;
    }

    /// <summary>
    /// Forwards the API data for processing with the given topic.
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
        if (trackStatusMessage is null)
        {
            _logger.LogError("[Formula 1] Invalid track status message received");
            return;
        }

        var messageArgs = new MessageEventArgs
        {
            Topic = "TrackStatus",
            Message = trackStatusMessage
        };

        MessageReceived?.Invoke(this, messageArgs);
    }

    /// <summary>
    /// Parses a race control message to a flag and relative data.
    /// </summary>
    /// <param name="data">Race control message data.</param>
    private void HandleRaceControlMessages(JsonNode data)
    {
        _logger.LogInformation("[Formula 1] Parsing race control message");
        var raceControlMessage = data["Messages"]?[0].Deserialize<RaceControlMessageDto>();
        if (raceControlMessage is null)
        {
            _logger.LogWarning("[Formula 1] Invalid race control message received");
            return;
        }

        var messageArgs = new MessageEventArgs
        {
            Topic = "RaceControlMessage",
            Message = raceControlMessage
        };

        MessageReceived?.Invoke(this, messageArgs);
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

        if (message.Status.Equals("finalised", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[Formula 1] Session finalised, stopping live timing");
            await OnSessionFinished();
        }

        var messageArgs = new MessageEventArgs
        {
            Topic = "SessionStatus",
            Message = message
        };

        MessageReceived?.Invoke(this, messageArgs);
    }

    /// <summary>
    /// Converts the input string to a <see cref="Flag"/>.
    /// </summary>
    /// <param name="input">The string representing a flag.</param>
    /// <param name="flag">
    /// When this method returns <see langword="true"/>, the related <see cref="Flag"/> item. Else
    /// <code>Flag.None</code> will be returned.
    /// </param>
    /// <returns>If the flag could be parsed.</returns>
    private static bool TryParseFlag(string? input, out Flag flag)
    {
        flag = input switch
        {
            "BLACK AND WHITE" => Flag.BlackWhite,
            "BLUE" => Flag.Blue,
            "CHEQUERED" => Flag.Chequered,
            "DOUBLE YELLOW" => Flag.DoubleYellow,
            "SLIPPERY SURFACE" => Flag.Surface,
            _ => Flag.None
        };

        return flag != Flag.None;
    }
}