using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RaceControl.SignalR;
using RaceControl.Track;
using Serilog;

namespace RaceControl.Category;

public partial class Formula2 : ICategory
{
    /// <summary>
    /// Flags to be ignored by race control message parser.
    /// </summary>
    private static readonly Flag[] IgnorableFlags = [Flag.Clear];

    /// <summary>
    /// Regex for checking if a race control message contains the message that the race/session will not resume.
    /// </summary>
    [GeneratedRegex("(?:WILL NOT).*(?:RESUME)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex NotResumeRegex();

    /// <summary>
    /// The SignalR <see cref="Client"/> connection object.
    /// </summary>
    private Client? _signalR;

    /// <summary>
    /// The record of the latest parsed flag.
    /// </summary>
    private static FlagData? _parsedFlag = new() { Flag = Flag.Chequered };

    /// <summary>
    /// To detect redundant calls.
    /// </summary>
    private bool _disposedValue;

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
        var feeds = new string[] {"trackfeed", "timefeed"};

        _signalR = new Client(
            _url,
            "streaming",
            ["F2", feeds],
            new(2, 1),
            "/streaming"
        );

        _signalR.AddHandler(string.Empty, string.Empty, HandleMessage);
        _signalR?.Start("GetData2");
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Stop()
    {
        throw new NotImplementedException();
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
            // TODO: add API connection to be disposed
        }

        _disposedValue = true;
    }

    /// <summary>
    /// Deconstructs the incoming message into an argument and payload. Calls the relating parsing method based on the
    /// argument.
    /// </summary>
    /// <param name="message">Message received from Formula 2 API.</param>
    private void HandleMessage(JsonArray message)
    {
        
    }

    /// <summary>
    /// Checks if the conditions allow it to parse race control messages.
    /// </summary>
    /// <returns>Can parse to race control message.</returns>
    private static bool ListenToRaceControlMessages => 
        _parsedFlag is { Flag: Flag.Chequered or Flag.Clear or Flag.Yellow };

    /// <summary>
    /// Checks if the given flag from a race control message should be ignored.
    /// </summary>
    /// <param name="flag">The parsed flag from a race control message.</param>
    /// <returns>If the flag should be ignored.</returns>
    private static bool IgnoreRaceControlFlag(Flag flag) =>
        _parsedFlag is not { Flag: Flag.Chequered } && IgnorableFlags.Contains(flag);
}