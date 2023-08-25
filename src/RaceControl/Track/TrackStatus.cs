namespace RaceControl.Track;

public sealed class TrackStatus
{
    /// <summary>
    /// Flag with their given priority.
    /// </summary>
    private static readonly Dictionary<Flag, short> FlagPriority = new()
    {
        { Flag.Blue, 1 },
        { Flag.Surface, 1 },
        { Flag.Clear, 1 },
        { Flag.Yellow, 2 },
        { Flag.DoubleYellow, 3 },
        { Flag.Vsc, 4 },
        { Flag.Code60, 4 },
        { Flag.Fyc, 4 },
        { Flag.SafetyCar, 5 },
        { Flag.Red, 6 },
        { Flag.Chequered, 8 }
    };

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

        var newFlagPrio = FlagPriority.GetValueOrDefault(data.Flag);
        var currentFlagPrio = FlagPriority.GetValueOrDefault(_activeFlag.Flag);
        if (data.Flag == Flag.Chequered)
        {
            _activeFlag = data;
            OnTrackFlagChange?.Invoke(_activeFlag);
            return;
        }

        if (_activeFlag.Flag == Flag.Clear && newFlagPrio == 1)
        {
            OnTrackFlagChange?.Invoke(data);
            return;
        }

        if (newFlagPrio < currentFlagPrio) return;

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