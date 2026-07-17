using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using RaceControl.SignalR;
using RaceControl.Track;

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
    public event EventHandler<FlagDataEventArgs>? FlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler? SessionFinished;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StartAsync(string session)
    {
        logger.LogInformation("[Formula 3] Starting API connection");
        var feeds = new[] {"status", "time"};

        _signalR = new Client(
            url,
            "streaming",
            ["F3", feeds],
            new Version(2, 1),
            "/streaming"
        );

        _signalR.Error += async _ => await OnSessionFinishedAsync().ConfigureAwait(false);
        //_signalR.AddHandler("Streaming", "timefeed", async message => await HandleTimefeedMessageAsync(message));
        _signalR.AddHandler("Streaming", "trackfeed", HandleTrackFeedMessage);
        _signalR.AddHandler("Streaming", "sessionfeed", async message => await HandleSessionFeedMessageAsync(message));

        await _signalR.StartAsync("JoinFeeds").ConfigureAwait(false);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StopAsync()
    {
        logger.LogInformation("[Formula 3] Closing API connection");
        _hasStarted = false;

        await _signalR!.StopAsync().ConfigureAwait(false);
        _signalR = null;

        if (null == FlagParsed)
            return;

        // Remove all the linked invocations of the FlagParsed event handler
        foreach (var del in FlagParsed.GetInvocationList())
            FlagParsed -= (EventHandler<FlagDataEventArgs>)del;

        if (null == SessionFinished)
            return;

        // Remove all the linked invocations of the SessionFinished event handler
        foreach (var del in SessionFinished.GetInvocationList())
            SessionFinished -= (EventHandler)del;
    }

    /// <summary>
    /// Invokes the FlagPares event with the required arguments
    /// </summary>
    /// <param name="flagData">The parsed flag.</param>
    protected virtual void OnFlagParsed(FlagData flagData)
    {
        var args = new FlagDataEventArgs { FlagData = flagData };

        FlagParsed?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes the SessionFinished event.
    /// </summary>
    protected virtual async Task OnSessionFinishedAsync()
    {

        SessionFinished?.Invoke(this, EventArgs.Empty);

        if (_signalR is not null && _hasStarted)
            await StopAsync().ConfigureAwait(false);
    }

        /// <summary>
    /// Parses the incoming Timing Feed message to check if the session is finished.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 3 API.</param>
    protected virtual async Task HandleTimefeedMessageAsync(JsonArray message)
    {
        logger.LogInformation("[Formula 3] Parsing time feed message");

        var sessionTimeData = message[2]?.Deserialize<string>();
        if (string.IsNullOrWhiteSpace(sessionTimeData))
        {
            logger.LogInformation("[Formula 3] Invalid session time received.");
            return;
        }

        var sessionTimeLeft = TimeSpan.ParseExact(sessionTimeData, "c", CultureInfo.InvariantCulture);

        // If the session has not jed finalized, stop the execution of the method.
        if (!_hasStarted || sessionTimeLeft != TimeSpan.Zero)
        {
            logger.LogInformation("[Formula 3] Session still active, remaining time left {time}", sessionTimeLeft.ToString("c"));
            return;
        }

        logger.LogInformation("[Formula 3] Session finalized, closing API connection");
        _hasStarted = false;
        OnFlagParsed(new FlagData { Flag = Flag.Chequered });
        await OnSessionFinishedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the incoming Tack Feed message to get the current flag of the session.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 2 API.</param>
    protected virtual void HandleTrackFeedMessage(JsonArray message)
    {
        logger.LogInformation("[Formula 3] Parsing track feed message");

        var data = message[1]?.Deserialize<TrackStatusMessage>();
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

        OnFlagParsed(new FlagData{ Flag = flag });
    }

    /// <summary>
    /// Parses the incoming Session Feed message to check if the session is finished.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 3 API.</param>

    protected virtual async Task HandleSessionFeedMessageAsync(JsonArray message)
    {
        logger.LogInformation("[Formula 3] Parsing session feed message");
        var data = message[1]?.Deserialize<SessionFeedMessage>();
        if (data is null) {
            logger.LogError("[Formula 3] Invalid session feed message received");
            return;
        }

        if (data.Value.Equals("Started", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 3] Session started");
            OnFlagParsed(new FlagData { Flag = Flag.Clear });
            _hasStarted = true;

            return;
        }

        if (data.Value.Equals("Finished", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Formula 3] Session finished");
            OnFlagParsed(new FlagData { Flag = Flag.Chequered });

            return;
        }

        if (data.Value.Equals("Finalised", StringComparison.OrdinalIgnoreCase) && _hasStarted)
        {
            logger.LogInformation("[Formula 3] Session finalized, closing API connection");

            OnFlagParsed(new FlagData { Flag = Flag.Clear });
            await OnSessionFinishedAsync().ConfigureAwait(false);

            return;
        }

        logger.LogInformation("[Formula 3] Session feed message ignored");
    }

    /// <summary>
    /// Structure of a track status message.
    /// </summary>
    private sealed record TrackStatusMessage(
        string Value,
        string Message
    );

    /// <summary>
    /// Structure of a session feed message.
    /// </summary>
    private sealed record SessionFeedMessage (
        string Value
    );
}