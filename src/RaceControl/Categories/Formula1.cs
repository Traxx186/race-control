using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RaceControl.SignalR;
using RaceControl.Track;

namespace RaceControl.Categories;

public partial class Formula1(ILogger logger, string url) : ICategory
{   
    /// <summary>
    /// How many times a <see cref="Flag.Chequered"/> needs to be received until the API 
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
    [GeneratedRegex("(?:WILL NOT).*(?:RESUME)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NotResumeRegex();
    
    /// <summary>
    /// The SignalR <see cref="Client"/> connection object.
    /// </summary>
    private Client? _signalR;

    /// <summary>
    /// How many <see cref="Flag.Chequered"/> are shown in the current session before the API connection
    /// needs to be closed.
    /// </summary>
    private int _numberOfChequered;

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
        logger.LogInformation("[Formula 1] Starting API connection");
        if (!SessionChequered.TryGetValue(session, out var numOfChequered))
        {
            logger.LogError("[Formula 1] Cannot find session {session}", session);
            return;
        }

        _signalR = new Client(
            url,
            "Streaming",
            ["RaceControlMessages", "TrackStatus"],
            new Version(1, 5)
        );

        _numberOfChequered = numOfChequered;

        _signalR.AddHandler("Streaming", "feed", HandleMessage);
        await _signalR.StartAsync("Subscribe");
    }

    public void Stop()
    {
        logger.LogInformation("[Formula 1] Closing API connection");
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
    /// Deconstructs the incoming message into an argument and payload. Calls the relating parsing method based on the
    /// argument.
    /// </summary>
    /// <param name="message">Message received from Formula 1 API.</param>
    private void HandleMessage(JsonArray message)
    {
        var argument = message[0]?.ToString() ?? string.Empty;
        var parsedFlag = argument switch
        {
            "TrackStatus" => ParseTrackStatusMessage(message[1]!),
            "RaceControlMessages" => ParseRaceControlMessage(message[1]!),
            _ => null
        };
        
        if (null == parsedFlag)
            return;

        if (parsedFlag.Flag is Flag.Chequered && --_numberOfChequered < 1)
            OnSessionFinished();

        logger.LogInformation("[Formula 1] New flag {flag}", parsedFlag.Flag);
        OnFlagParsed(parsedFlag);
    }

    /// <summary>
    /// Parses a track status message to a flag and relative data.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Parsed flag.</returns>
    private FlagData? ParseTrackStatusMessage(JsonNode message)
    {
        logger.LogInformation("[Formula 1] Parsing track status message");
        var data = message.Deserialize<TrackStatusMessage>();
        if (data == null || !short.TryParse(data.Status, out var status))
        {
            logger.LogError("[Formula 1] Invalid track status message received");
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
    private FlagData? ParseRaceControlMessage(JsonNode message)
    {
        logger.LogInformation("[Formula 1] Parsing race control message");

        var data = message["Messages"]?.ToJsonString();
        if (null == data)
        {
            logger.LogInformation("[Formula 1] Race control message could not be parsed");
            return null;
        }

        // Extract the race control message object from the SignalR message. If it is the first message
        // of the session, different extraction is needed.
        if (data.StartsWith('['))
        {
            data = data.TrimStart('[').TrimEnd(']');
        }
        else
        {
            data = data.Split(':', 2)[1];
            data = data.Remove(data.Length - 1);
        } 

        // Parse the extracted message to the RaceControlMessage record
        var raceControlMessage = JsonSerializer.Deserialize<RaceControlMessage>(data);
        if (null == raceControlMessage)
        {
            logger.LogWarning("[Formula 1] Race control message could not be parsed");
            return null;
        }

        // Checks if the slippery surface flag is shown.
        if (raceControlMessage.Message.Contains("SLIPPERY"))
        {
            logger.LogInformation("[Formula 1] Parsed race control message to {flag}", Flag.Surface);
            return new FlagData { Flag = Flag.Surface };
        }

        // Checks if the session will not be resumed.
        if (NotResumeRegex().IsMatch(raceControlMessage.Message))
        {
            logger.LogInformation("[Formula 1] Session will not be resumed, setting current flag to {flag}", Flag.Chequered);
            return new FlagData { Flag = Flag.Chequered };
        }

        // If the message category is not 'Flag', or received clear message, the message can be ignored.
        if (raceControlMessage is not { Category: "Flag" } or { Flag: "CLEAR" })
        {
            logger.LogInformation("[Formula 1] Race control message ignored");
            return null;
        }

        // Checks if the flag message contains a valid flag and if the flag should be ignored.
        if (!TrackStatus.TryParseFlag(raceControlMessage.Flag, out var flag))
        {
            logger.LogWarning("[Formula 1] Could not parse flag '{flag}'", raceControlMessage.Flag);
            return null;
        }

        if (!int.TryParse(raceControlMessage.RacingNumber, out var driver))
            driver = 0;
        
        return new FlagData { Flag = flag, Driver = driver };
    }

    /// <summary>
    /// Structure of a track status message.
    /// </summary>
    private sealed record TrackStatusMessage(
        string Status,
        string Message
    );

    /// <summary>
    /// Structure of a race control message.
    /// </summary>
    private sealed record RaceControlMessage(
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