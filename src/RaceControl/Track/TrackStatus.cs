using RaceControl.Services;

namespace RaceControl.Track;

public sealed class TrackStatus(ILogger<TrackStatus> logger, WebsocketService websocketService)
{
    /// <summary>
    /// Flag with their given priority. Flags with priority 0 are information flags
    /// </summary>
    private static readonly Dictionary<Flag, short> FlagPriority = new()
    {
        { Flag.BlackWhite, 0 },
        { Flag.Blue, 0 },
        { Flag.Surface, 0 },
        { Flag.Yellow, 2 },
        { Flag.DoubleYellow, 3 },
        { Flag.Vsc, 4 },
        { Flag.Code60, 4 },
        { Flag.Fyc, 4 },
        { Flag.SafetyCar, 5 },
        { Flag.Red, 6 },
    };

    /// <summary>
    /// Flags that override the other race flags.
    /// </summary>
    private static readonly Flag[] OverrideFlags = [Flag.Clear, Flag.Chequered];

    /// <summary>
    /// The current active flag of the session.
    /// </summary>
    public FlagData ActiveFlag { get; private set; } = new() { Flag = Flag.Clear };

    /// <summary>
    /// Sets the current active flag. If the priority of the given flag equals 1, the OnFlagChange event will be called
    /// but the flag data will not be saved.
    /// </summary>
    /// <param name="data">Flag data to be processed.</param>
    public async Task SetActiveFlagAsync(FlagData data)
    {
        logger.LogInformation("[Track Status] New flag received");
        if (OverrideFlags.Contains(data.Flag))
        {
            logger.LogInformation("[Track Status] Received override flag {flag}, sending flag and updating track status", data.Flag);
            ActiveFlag = data;
            await websocketService.BroadcastEventAsync(MessageEvent.FlagChange, ActiveFlag, CancellationToken.None);
            
            return;
        }

        // If given flag is the same as the active flag, or the active flag is
        // None. Do not try to set the given flag.
        if (data.Flag == ActiveFlag.Flag || data.Flag == Flag.None)
            return;

        var newFlagPrio = FlagPriority.GetValueOrDefault(data.Flag);
        var currentFlagPrio = FlagPriority.GetValueOrDefault(ActiveFlag.Flag); 
        if (ActiveFlag.Flag == Flag.Clear && ActiveFlag.Flag != Flag.Chequered && newFlagPrio == 0)
        {
            logger.LogInformation("[Track Status] Received information flag, sending flag data but not updating track status");
            await websocketService.BroadcastEventAsync(MessageEvent.FlagChange, data, CancellationToken.None);
            return;
        }

        logger.LogInformation("[Track Status] Received status flag");
        if (newFlagPrio < currentFlagPrio)
        {
            logger.LogInformation("[Track Status] New received status flag has lower priority, ignoring flag");
            return;
        }

        logger.LogInformation("[Track Status] New received status flag with higher priority, updating track status");
        ActiveFlag = data;
        await websocketService.BroadcastEventAsync(MessageEvent.FlagChange, ActiveFlag, CancellationToken.None);
    }

    /// <summary>
    /// Converts the input string to a <see cref="Flag"/>. 
    /// </summary>
    /// <param name="input">The string representing a flag.</param>
    /// <param name="flag">
    /// When this method returns <see langword="true"/>, the related <see cref="Flag"/> item.
    /// Else <code>Flag.None</code> will be returned.
    /// </param>
    /// <returns>If the flag could be parsed.</returns>
    public static bool TryParseFlag(string? input, out Flag flag)
    {
        flag = input switch
        {
            "BLACK AND WHITE" => Flag.BlackWhite,
            "BLUE" => Flag.Blue,
            "CHEQUERED" => Flag.Chequered,
            "CLEAR" or "GREEN" => Flag.Clear,
            "CODE 60" => Flag.Code60,
            "DOUBLE YELLOW" => Flag.DoubleYellow,
            "FULL COURSE YELLOW" => Flag.Fyc,
            "RED" => Flag.Red,
            "SAFETY CAR" => Flag.SafetyCar,
            "SLIPPERY SURFACE" => Flag.Surface,
            "VIRTUAL SAFETY CAR" => Flag.Vsc,
            "YELLOW" => Flag.Yellow,
            _ => Flag.None
        };
        
        return flag != Flag.None;
    }
}