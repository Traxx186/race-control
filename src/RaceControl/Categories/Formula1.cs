using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR.Client;
using RaceControl.Track;

namespace RaceControl.Categories;

public partial class Formula1(ILogger? logger = null) : ICategory
{
    private const string LiveTimingUrl = "https://livetiming.formula1.com/signalrcore";

    /// <summary>
    /// How many times a <see cref="Flag.Chequered"/> needs to be received until the API connections needs to be
    /// disconnected.
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
    /// The SignalR <see cref="HubConnection"/> connection object.
    /// </summary>
    private HubConnection? _signalR;

    /// <summary>
    /// How many <see cref="Flag.Chequered"/> are shown in the current session before the API connection
    /// needs to be closed.
    /// </summary>
    private int _numberOfChequered;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler<FlagDataEventArgs>? FlagParsed;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event EventHandler? SessionFinished;

    /// <summary>
    /// If the live timing API is active.
    /// </summary>
    public bool Connected => _signalR?.State == HubConnectionState.Connected;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StartAsync(string session)
    {
        logger?.LogInformation("[Formula 1] Starting API connection");
        if (!SessionChequered.TryGetValue(session, out _numberOfChequered))
        {
            logger?.LogError("[Formula 1] Cannot find session {session}", session);
            SessionFinished?.Invoke(this, EventArgs.Empty);

            return;
        }

        logger?.LogInformation("[Formula 1] Connect to {url}",  LiveTimingUrl);
        _signalR = new HubConnectionBuilder()
            .WithUrl(LiveTimingUrl)
            .WithAutomaticReconnect()
            .Build();

        _signalR.Closed += async _ =>
        {
            logger?.LogInformation("[Formula 1] API connection terminated");
            await OnSessionFinished();
        };

        _signalR.On<TrackStatusMessage>("TrackStatus", async message => await HandleTrackStatusMessageAsync(message));
        _signalR.On<RaceControlMessage>("RaceControlMessages", async message => await HandleRaceControlMessageAsync(message));

        await _signalR.StartAsync();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task StopAsync()
    {
        logger?.LogInformation("[Formula 1] Closing API connection");
        await _signalR?.StopAsync()!;

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
    protected virtual async Task OnFlagParsed(FlagData flagData)
    {
        var args = new FlagDataEventArgs { FlagData = flagData };
        FlagParsed?.Invoke(this, args);

        if (flagData.Flag is Flag.Chequered && --_numberOfChequered < 1)
            await OnSessionFinished();
    }

    /// <summary>
    /// Invokes the SessionFinished event.
    /// </summary>
    protected virtual async Task OnSessionFinished()
    {
        if (_signalR?.State == HubConnectionState.Connected)
            await StopAsync();

        SessionFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Parses a track status message to a flag and relative data.
    /// </summary>
    /// <param name="trackStatusMessage">Message object.</param>
    /// <returns>Parsed flag.</returns>
    private async Task HandleTrackStatusMessageAsync(TrackStatusMessage trackStatusMessage)
    {
        logger?.LogInformation("[Formula 1] Parsing track status message");
        if (!short.TryParse(trackStatusMessage.Status, out var status))
        {
            logger?.LogError("[Formula 1] Invalid track status message received");
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

        await OnFlagParsed(new FlagData { Flag = flag });
    }

    /// <summary>
    /// Parses a race control message to a flag and relative data.
    /// </summary>
    /// <param name="raceControlMessage">Message object.</param>
    private async Task HandleRaceControlMessageAsync(RaceControlMessage raceControlMessage)
    {
        logger?.LogInformation("[Formula 1] Parsing race control message");

        // Checks if the slippery surface flag is shown.
        if (raceControlMessage.Message.Contains("slippery", StringComparison.CurrentCultureIgnoreCase))
        {
            logger?.LogInformation("[Formula 1] Parsed race control message to {flag}", Flag.Surface);
            await OnFlagParsed(new FlagData { Flag = Flag.Surface });

            return;
        }

        // Checks if the session will be postponed.
        if (raceControlMessage.Message.Contains("postponed", StringComparison.CurrentCultureIgnoreCase))
        {
            logger?.LogInformation("[Formula 1] Session will be postponed, setting current flag to {flag}", Flag.Chequered);

            // Because a postponed session will be rescheduled, sub sessions like those in qualifying will not take
            // place. Therefore, setting the numOfChequered to 0 is required in order to stop the API connection.
            _numberOfChequered = 0;

            await OnFlagParsed(new FlagData { Flag = Flag.Chequered });
            return;
        }

        // Checks if the session will not be resumed.
        if (NotResumeRegex().IsMatch(raceControlMessage.Message))
        {
            logger?.LogInformation("[Formula 1] Session will not be resumed, setting current flag to {flag}", Flag.Chequered);
            await OnFlagParsed(new FlagData { Flag = Flag.Chequered });
            return;
        }

        // If the message category is not 'Flag', or received clear message, the message can be ignored.
        if (raceControlMessage is not { Category: "Flag" } or { Flag: "CLEAR" })
        {
            logger?.LogInformation("[Formula 1] Race control message ignored");
            return;
        }

        // Checks if the flag message contains a valid flag and if the flag should be ignored.
        if (!TrackStatus.TryParseFlag(raceControlMessage.Flag, out var flag))
        {
            logger?.LogWarning("[Formula 1] Could not parse flag '{flag}'", raceControlMessage.Flag);
            return;
        }

        if (!int.TryParse(raceControlMessage.RacingNumber, out var driver))
            driver = 0;

        await OnFlagParsed(new FlagData { Flag = flag, Driver = driver == 0 ? null : driver });
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