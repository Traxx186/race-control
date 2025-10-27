namespace RaceControl.Track;

/// <summary>
/// Object that contains a flag and the related driver.
/// </summary>
public class FlagData
{
    /// <summary>
    /// The <see cref="Flag"/>
    /// </summary>
    public Flag Flag { get; init; }

    /// <summary>
    /// The driver number that related to the flag
    /// </summary>
    public int? Driver { get; init; }
}

public class FlagDataEventArgs : EventArgs
{
    public required FlagData FlagData { get; init; }
}