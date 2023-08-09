namespace RaceControl.Track;

/// <summary>
/// Object that contains a flag and the related driver.
/// </summary>
public record FlagData
{
    /// <summary>
    /// The <see cref="Flag"/>
    /// </summary>
    public Flag Flag;
    
    /// <summary>
    /// The number of the driver that related to te flag
    /// </summary>
    public int? Driver;
}