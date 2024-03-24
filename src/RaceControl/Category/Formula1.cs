using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RaceControl.SignalR;
using RaceControl.Track;
using Serilog;

namespace RaceControl.Category;

public partial class Formula1 : ICategory
{
    /// <summary>
    /// Flags to be ignored by race control message parser.
    /// </summary>
    private static readonly Flag[] IgnorableFlags = [Flag.Clear];
    
    /// <summary>
    /// Data streams to listen to and the related method to be called.
    /// </summary>
    private static readonly Dictionary<string, Func<JsonNode, FlagData?>> DataStreams = new()
    {
        { "TrackStatus", ParseTrackStatusMessage },
        { "RaceControlMessages", ParseRaceControlMessage }
    };

    /// <summary>
    /// How many times a <see cref="Flag.Chequered"/> needs to be recieved until the API 
    /// connections needs to be broken.
    /// </summary>
    private static readonly Dictionary<string, int> SessionChequered = new()
    {
        { "fp1", 1 },
        { "fp2", 1 },
        { "fp3", 1 },
        { "qualifying", 3 },
        { "sprintQualifying", 3 },
        { "sprint", 1 },
        { "gp", 1 }
    };
    
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
    /// How many <see cref="Flag.Chequered"/> are shown in the current sessnion before the API connection
    /// needs to be closed.
    /// </summary>
    private int _numberOfChequered;

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
    public event Action<FlagData>? OnFlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event Action? OnSessionFinished;

    public Formula1(string url)
    {
        _url = url;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Start(string session)
    {
        Log.Information("[Formula 1] Starting API connection");
        if (!SessionChequered.TryGetValue(session, out var numOfChequered))
        {
            Log.Error($"[Formula 1] Cannot find session {session}");
            return;
        }

        _signalR = new Client(
            _url,
            "Streaming",
            ["RaceControlMessages", "TrackStatus"],
            new(1, 5)
        );

        _signalR.AddHandler("Streaming", "feed", HandleMessage);

        _numberOfChequered = numOfChequered;
        _signalR?.Start("Subscribe");
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Stop()
    {
        Log.Information("[Formula 1] Closing API connection");
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
    /// Deconstructs the incoming message into an argument and payload. Calls the relating parsing method based on the
    /// argument.
    /// </summary>
    /// <param name="message">Message received from Formula 1 API.</param>
    private void HandleMessage(JsonArray message)
    {
        var argument = message[0]?.ToString() ?? string.Empty;
        if (!DataStreams.TryGetValue(argument, out var callable))
            return;

        var parsedFlag = callable.Invoke(message[1]);
        if (null == parsedFlag)
            return;

        if (parsedFlag.Flag is Flag.Chequered && --_numberOfChequered < 1)
            OnSessionFinished?.Invoke();

        Log.Information($"[Formula 1] New flag {parsedFlag.Flag}");
        OnFlagParsed?.Invoke(parsedFlag);
    }

    /// <summary>
    /// Parses a track status message to a flag and relative data.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Parsed flag.</returns>
    private static FlagData? ParseTrackStatusMessage(JsonNode message)
    {
        Log.Information("[Formula 1] Parsing track status message");
        var data = message.Deserialize<TrackStatusMessage>();
        if (data == null || !short.TryParse(data.Status, out var status))
        {
            Log.Error("[Formula 1] Invalid track status message recieved");
            return null;
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

        return new FlagData { Flag = flag };
    }

    /// <summary>
    /// Parses a race control message to a flag and relative data.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Parsed flag.</returns>
    private static FlagData? ParseRaceControlMessage(JsonNode message)
    {
        Log.Information("[Formula 1] Parsing race control message");

        var data = message["Messages"]?.ToJsonString();
        if (null == data)
        {
            Log.Warning("[Formula 1] Race control message could not be parsed");
            return null;
        }

        // Extract the race control message object from the SignalR message.
        data = data.StartsWith('[')
            ? data.TrimStart('[').TrimEnd(']')
            : data.Split(':', 2)[1];

        // Parse the extracted message to the RaceControlMessage record
        var raceControlMessage = JsonSerializer.Deserialize<RaceControlMessage>(data.Remove(data.Length - 1));
        if (null == raceControlMessage)
        {
            Log.Warning("[Formula 1] Race control message could not be parsed");
            return null;
        }

        // Checks if the slippery surface flag is shown.
        if (raceControlMessage.Message.Contains("SLIPPERY"))
        {
            Log.Information($"[Formula 1] Parsed race control message to {Flag.Surface}");
            return new FlagData { Flag = Flag.Surface };
        }

        // Checks if the session will not be resumed.
        if (NotResumeRegex().IsMatch(raceControlMessage.Message))
        {
            Log.Information($"[Formula 1] Session will not be resumed, setting current flag to {Flag.Chequered}");
            return new FlagData { Flag = Flag.Chequered };
        }

        // If the message category is not 'Flag', or recieved clear message, the message can be ignored.
        if (raceControlMessage is not { Category: "Flag" } || raceControlMessage is { Flag: "CLEAR" })
        {
            Log.Information("[Formula 1] Race control message ignored");
            return null;
        }

        // Checks if the flag message contains a valid flag and if the flag should be ignored.
        if (!TrackStatus.TryParseFlag(raceControlMessage.Flag, out var flag))
        {
            Log.Warning($"[Formula 1] Could not parse flag '{raceControlMessage.Flag}'");
            return null;
        }

        if (!int.TryParse(raceControlMessage.RacingNumber, out var driver))
            driver = 0;
        
        return new FlagData { Flag = flag, Driver = driver };
    }

    /// <summary>
    /// Structure of a track status message.
    /// </summary>
    private sealed record class TrackStatusMessage(
        string Status,
        string Message
    );

    /// <summary>
    /// Structure of a race control message.
    /// </summary>
    private sealed record class RaceControlMessage(
        DateTime Utc,
        int Lap,
        string Category,
        string Message,
        string Flag,
        string Scope,
        string RacingNumber,
        int Sector,
        string Mode
    );
}