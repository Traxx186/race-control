using Serilog;

namespace RaceControl.Track;

public sealed class TrackStatus
{
    /// <summary>
    /// Flag with their given priority. Flags with priority 0 are information flags
    /// </summary>
    private static readonly Dictionary<Flag, short> FlagPriority = new()
    {
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
    private static readonly Flag[] OverrideFlags = { Flag.Clear, Flag.Chequered };
    
    /// <summary>
    /// The current active flag of the session.
    /// </summary>
    private FlagData _activeFlag = new() { Flag = Flag.Chequered };

    /// <summary>
    /// Event that gets called when the flag of the active session changes.
    /// </summary>
    public event Action<FlagData>? OnTrackFlagChange;

    /// <summary>
    /// Sets the current active flag. If the priority of the given flag equals 1, the OnFlagChange event will be called
    /// but the flag data will not be saved.
    /// </summary>
    /// <param name="data">Flag data to be processed.</param>
    public void SetActiveFlag(FlagData data)
    {
        if (data.Flag == _activeFlag.Flag)
            return;

        Log.Information("[Track Status] New flag received");
        var newFlagPrio = FlagPriority.GetValueOrDefault(data.Flag);
        var currentFlagPrio = FlagPriority.GetValueOrDefault(_activeFlag.Flag);
        if (OverrideFlags.Contains(data.Flag))
        {
            Log.Information($"[Track Status] Received override flag {data.Flag}, sending flag and updating track status");
            _activeFlag = data;
            OnTrackFlagChange?.Invoke(_activeFlag);
            
            return;
        }

        if (_activeFlag.Flag == Flag.Clear && newFlagPrio == 0)
        {
            Log.Information("[Track Status] Received information flag, sending flag data but not updating track status");
            OnTrackFlagChange?.Invoke(data);
            return;
        }

        Log.Information("[Track Status] Received status flag");
        if (newFlagPrio < currentFlagPrio)
        {
            Log.Information("[Track Status] New received status flag has lower priority, ignoring flag");
            return;
        }

        Log.Information("[Track Status] New received status flag with higher priority, updating track status");
        _activeFlag = data;
        OnTrackFlagChange?.Invoke(_activeFlag);
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
            "BLUE" => Flag.Blue,
            "CHEQUERED" => Flag.Chequered,
            "CLEAR" => Flag.Clear,
            "GREEN" => Flag.Clear,
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