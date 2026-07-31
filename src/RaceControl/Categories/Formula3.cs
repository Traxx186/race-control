using System.Text.Json;
using System.Text.Json.Nodes;
using RaceControl.Data.Dtos.LiveTimingDtos;
using RaceControl.Data.Enums;
using RaceControl.Data.Events;
using RaceControl.SignalR;

namespace RaceControl.Categories;

public class Formula3(ILogger logger, string url) : ICategory
{
    /// <summary>
    /// The SignalR <see cref="Client"/> connection object.
    /// </summary>
    private Client? _signalR;

    /// <summary>
    /// If the session has actually started.
    /// </summary>
    private bool _hasStarted;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler<FlagChangedEventArgs>? FlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler? SessionFinished;

    /// <inheritdoc/>
    public bool Connected => _signalR?.Running ?? false;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StartAsync(string session)
    {
        logger.LogInformation("[Formula 3] Starting API connection");
        var feeds = new[] {"status"};

        _signalR = new Client(
            url,
            "streaming",
            ["F3", feeds],
            new Version(2, 1),
            "/streaming"
        );

        _signalR.Error += async _ => await OnSessionFinishedAsync();
        _signalR.AddHandler("Streaming", "trackfeed", HandleTrackFeedMessage);
        _signalR.AddHandler("Streaming", "sessionfeed", async message => await HandleSessionFeedMessageAsync(message));

        await _signalR.StartAsync("JoinFeeds");
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StopAsync()
    {
        FlagParsed = null;

        if (!_hasStarted || _signalR is null)
            return;

        logger.LogInformation("[Formula 3] Closing API connection");
        _hasStarted = false;

        await _signalR!.StopAsync();
        _signalR = null;
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
    private async Task OnSessionFinishedAsync()
    {
        await StopAsync();

        SessionFinished?.Invoke(this, EventArgs.Empty);
        SessionFinished = null;
    }

    /// <summary>
    /// Parses the incoming Tack Feed message to get the current flag of the session.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 2 API.</param>
    private void HandleTrackFeedMessage(JsonArray message)
    {
        logger.LogInformation("[Formula 3] Parsing track feed message");

        var data = message[1]?.Deserialize<TrackStatusMessageDto>();
        if (!short.TryParse(data?.Value, out var status))
        {
            logger.LogError("[Formula 3] Invalid track status message received");
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
    /// Parses the incoming Session Feed message to check if the session is finished.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 3 API.</param>

    private async Task HandleSessionFeedMessageAsync(JsonArray message)
    {
        logger.LogInformation("[Formula 3] Parsing session feed message");
        var data = message[1]?.Deserialize<SessionStatusMessageDto>();
        if (data is null) {
            logger.LogError("[Formula 3] Invalid session feed message received");
            return;
        }

        if (data.Value.Equals("Started", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 3] Session started");
            OnFlagParsed(Flag.Clear);
            _hasStarted = true;

            return;
        }

        if (data.Value.Equals("Finished", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 3] Session finished");
            OnFlagParsed(Flag.Chequered);

            return;
        }

        if (data.Value.Equals("Finalised", StringComparison.OrdinalIgnoreCase) && _hasStarted)
        {
            logger.LogInformation("[Formula 3] Session finalized, closing API connection");
            await OnSessionFinishedAsync();

            return;
        }

        logger.LogInformation("[Formula 3] Session feed message ignored");
    }
}