using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RaceControl.SignalR;
using RaceControl.Track;
using Serilog;

namespace RaceControl.Category;

public sealed partial class Formula1 : ICategory
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
    /// Regex for checking if a race control message contains the message that the race/session will not resume.
    /// </summary>
    [GeneratedRegex("^[RSQ0-9].+\\b(?:WILL NOT)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex NotResumeRegex();
    
    /// <summary>
    /// The SignalR <see cref="Client"/> connection object.
    /// </summary>
    private readonly Client _signalR;

    /// <summary>
    /// The record of the latest parsed flag.
    /// </summary>
    private static FlagData? _parsedFlag = new() { Flag = Flag.Chequered };

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
        _signalR = new Client(
            url,
            "Streaming",
            ["RaceControlMessages", "TrackStatus"]
        );

        _signalR.AddHandler("Streaming", "feed", HandleMessage);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Start(string session)
    {
        Log.Information("[Formula 1] Starting API connection");
        _signalR.Start();
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

        _parsedFlag = parsedFlag;

        Log.Information($"[Formula 1] New flag {_parsedFlag.Flag}");
        OnFlagParsed?.Invoke(_parsedFlag);
    }

    /// <summary>
    /// Parses a track status message to a flag and relative data.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Parsed flag.</returns>
    private static FlagData ParseTrackStatusMessage(JsonNode message)
    {
        Log.Information("[Formula 1] Parsing track status message");
        var data = message.GetValue<TrackStatusMessage>();
        var flag = data.Status switch
        {
            2 => Flag.Yellow,
            4 => Flag.SafetyCar,
            5 => Flag.Red,
            6 => Flag.Vsc,
            _ => Flag.Clear
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
        if (!ListenToRaceControlMessages)
            return null;

        Log.Information("[Formula 1] Parsing race control message");
        var data = message["Messages"]?.AsArray()[0]?.GetValue<RaceControlMessage>();
        if (null == data)
        {
            Log.Warning("[Formula 1] Race control message could not be parsed");
            return null;
        }
        
        // Checks if the slippery surface flag is shown.
        if (data.Value.Message.Contains("SLIPPERY"))
        {
            Log.Information($"[Formula 1] Parsed race control message to {Flag.Surface}");
            return new FlagData { Flag = Flag.Surface };
        }

        // Checks if the session will not be resumed.
        if (NotResumeRegex().IsMatch(data.Value.Message))
        {
            Log.Information($"[Formula 1] Session will not be resumed, setting current flag to {Flag.Chequered}");
            return new FlagData { Flag = Flag.Chequered };
        }

        // If the message category is not 'Flag', the message can be ignored.
        if (data.Value is not { Category: "Flag" })
        {
            Log.Information("[Formula 1] Race control message ignored");
            return null;
        }

        // Checks if the flag message contains a valid flag and if the flag should be ignored.
        if (!TrackStatus.TryParseFlag(data.Value.Flag, out var flag) || IgnoreRaceControlFlag(flag))
        {
            if (flag == Flag.None)
                Log.Warning($"[Formula 1] Could not parse flag '{data.Value.Flag}'");
            else 
                Log.Information("[Formula 1] Parsed flag ignored");
            
            return null;
        }

        var driver = flag == Flag.Blue
            ? data.Value.RacingNumber
            : 0;
        
        return new FlagData { Flag = flag, Driver = driver };
    }

    /// <summary>
    /// Checks if the conditions allow it to parse race control messages.
    /// </summary>
    /// <returns>Can parse to race control message.</returns>
    private static bool ListenToRaceControlMessages => 
        _parsedFlag is { Flag: Flag.Chequered or Flag.Clear or Flag.Yellow };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flag"></param>
    /// <returns></returns>
    private static bool IgnoreRaceControlFlag(Flag flag) =>
        _parsedFlag is not { Flag: Flag.Chequered } && IgnorableFlags.Contains(flag);
        
    
    /// <summary>
    /// Structure of a track status message.
    /// </summary>
    private struct TrackStatusMessage
    {
        public short Status;
        public string Message;
    }

    /// <summary>
    /// Structure of a race control message.
    /// </summary>
    private struct RaceControlMessage
    {
        public DateTime Utc;
        public int Lap;
        public string Category;
        public string Message;
        public string Flag;
        public string Scope;
        public int RacingNumber;
        public int Sector;
        public string Mode;
    }
}