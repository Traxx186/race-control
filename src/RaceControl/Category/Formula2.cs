using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using RaceControl.SignalR;
using RaceControl.Track;
using Serilog;

namespace RaceControl.Category;

public partial class Formula2 : ICategory
{
    /// <summary>
    /// The SignalR <see cref="Client"/> connection object.
    /// </summary>
    private Client? _signalR;

    /// <summary>
    /// To detect redundant calls.
    /// </summary>
    private bool _disposedValue;

    /// <summary>
    /// If the session has actually started.
    /// </summary>
    private bool _hasStarted;

    /// <summary>
    /// The URL to the live timing API.
    /// </summary>
    private readonly string _url;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event Action<FlagData> OnFlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event Action OnSessionFinished;
    
    public Formula2(string url)
    {
        _url = url;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Start(string session)
    {
        Log.Information("[Formula 2] Starting API connection");
        var feeds = new string[] {"status", "time"};

        _signalR = new Client(
            _url,
            "streaming",
            ["F2", feeds],
            new(2, 1),
            "/streaming"
        );

        _signalR.AddHandler("Streaming", "timefeed", HandleTimefeedMessage);
        _signalR.AddHandler("Streaming", "trackfeed", HandleTrackFeedMessage);
        _signalR.AddHandler("Streaming", "sessionfeed", HandleSessionFeedMessage);
        _signalR?.Start("JoinFeeds");
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Stop()
    {
        Log.Information("[Formula 2] Closing API connection");
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
        if (_disposedValue)
            return;

        if (disposing)
        {
            _signalR?.Stop();
            _signalR = null;
        }

        _disposedValue = true;
    }

    /// <summary>
    /// Parses the incomming Timing Feed message to check if the session is finished.
    /// </summary>
    /// <param name="data">Message argument data received from Formula 2 API.</param>
    private void HandleTimefeedMessage(JsonArray message)
    {
        Log.Information("[Formula 2] Parsing time feed message");

        var sessionTimeData = message[2]?.Deserialize<string>();
        if (string.IsNullOrWhiteSpace(sessionTimeData))
        {
            Log.Error("[Formula 2] Invalid session time received.");
            return;
        }

        var sessionTimeLeft = TimeSpan.ParseExact(sessionTimeData, "c", CultureInfo.InvariantCulture);

        // Send session finished event if the session has started and the finish signal is send.
        if (_hasStarted && sessionTimeLeft == TimeSpan.Zero)
        {   
            Log.Information("[Formula 2] Session finalised, closing API connection");

            _hasStarted = false;
            OnFlagParsed?.Invoke(new FlagData { Flag = Flag.Chequered });
            OnSessionFinished?.Invoke();
        }
    }

    /// <summary>
    /// Parses the incomming Tack Feed message to get the current flag of the session.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 2 API.</param>
    private void HandleTrackFeedMessage(JsonArray message)
    {
        Log.Information("[Formula 2] Parsing track feed message");

        var data = message[1]?.Deserialize<TrackStatusMessage>();
        if (data == null || !short.TryParse(data.Value, out var status))
        {
            Log.Error("[Formula 2] Invalid track status message recieved");
            return;
        }

        var flag = status switch 
        {
            2 => Flag.Yellow,
            4 => Flag.SafetyCar,
            5 => Flag.Red,
            6 or 7 => Flag.Vsc,
            _ => Flag.Clear
        };

        OnFlagParsed?.Invoke(new FlagData{ Flag = flag });
    }

    /// <summary>
    /// Parses the incomming Session Feed message to check if the session is finished.
    /// </summary>
    /// <param name="message">Message argument data received from Formula 2 API.</param>

    private void HandleSessionFeedMessage(JsonArray message)
    {
        Log.Information("[Formula 2] Parsing session feed message");
        var data = message[1]?.Deserialize<SessionFeedMessage>();
        if (data == null) {
            Log.Error("[Formula 2] Invalid session feed message recieved");
            return;
        }

        switch (data.Value.ToLower())
        {
            case "started":
                Log.Information("[Formula 2] Session started");
                OnFlagParsed?.Invoke(new FlagData { Flag = Flag.Clear });
                _hasStarted = true;

                break;
            case "finished":
            case "finalised":
                if (!_hasStarted)
                    break;

                Log.Information("[Formula 2] Session finalised, closing API connection");

                _hasStarted = false;
                OnFlagParsed?.Invoke(new FlagData { Flag = Flag.Chequered });
                OnSessionFinished?.Invoke();

                break;
            default:
                Log.Information("[Formula 2] Session feed message ignored");
                break;
        }       
    }

    /// <summary>
    /// Structure of a track status message.
    /// </summary>
    private sealed record class TrackStatusMessage(
        string Value,
        string Message
    );

    /// <summary>
    /// Structure of a session feed message.
    /// </summary>
    private sealed record class SessionFeedMessage (
        string Value
    );
}