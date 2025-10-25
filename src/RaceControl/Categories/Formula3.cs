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
    /// If the object has already been disposed.
    /// </summary>
    private bool _disposed;
    
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
        
        _signalR.AddHandler("Streaming", "timefeed", HandleTimefeedMessage);
        _signalR.AddHandler("Streaming", "trackfeed", HandleTrackFeedMessage);
        _signalR.AddHandler("Streaming", "sessionfeed", HandleSessionFeedMessage);
        await _signalR.StartAsync("JoinFeeds");
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Stop()
    {
        logger.LogInformation("[Formula 3] Closing API connection");
        Dispose();
    }
    
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) 
            return;

        if (disposing)
        {
            _signalR?.Stop();
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

        _disposed = true;
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
    protected virtual void OnSessionFinished()
    {
        SessionFinished?.Invoke(this, EventArgs.Empty);
    }
    
        /// <summary>
    /// Parses the incoming Timing Feed message to check if the session is finished.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 3 API.</param>
    protected virtual void HandleTimefeedMessage(JsonArray message)
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
        OnSessionFinished();
    }

    /// <summary>
    /// Parses the incoming Tack Feed message to get the current flag of the session.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 2 API.</param>
    protected virtual void HandleTrackFeedMessage(JsonArray message)
    {
        logger.LogInformation("[Formula 3] Parsing track feed message");

        var data = message[1]?.Deserialize<TrackStatusMessage>();
        if (data == null || !short.TryParse(data.Value, out var status))
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

    protected virtual void HandleSessionFeedMessage(JsonArray message)
    {
        logger.LogInformation("[Formula 3] Parsing session feed message");
        var data = message[1]?.Deserialize<SessionFeedMessage>();
        if (data == null) {
            logger.LogError("[Formula 3] Invalid session feed message received");
            return;
        }

        switch (data.Value.ToLower())
        {
            case "started":
                logger.LogInformation("[Formula 3] Session started");
                OnFlagParsed(new FlagData { Flag = Flag.Clear });
                _hasStarted = true;

                break;
            case "finished":
            case "finalised":
                if (!_hasStarted)
                    break;

                logger.LogInformation("[Formula 3] Session finalized, closing API connection");

                _hasStarted = false;
                OnFlagParsed(new FlagData { Flag = Flag.Chequered });
                OnSessionFinished();

                break;
            default:
                logger.LogInformation("[Formula 3] Session feed message ignored");
                break;
        }       
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