using System.Text.Json.Serialization;

namespace RaceControl.Track;

/// <summary>
/// Object that contains a flag and the related driver.
/// </summary>
public class FlagData : ICloneable
{
    /// <summary>
    /// The <see cref="Flag"/>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Flag Flag;

    /// <summary>
    /// The driver number that related to the flag
    /// </summary>
    public int Driver;

    public object Clone()
    {
        var flagData = new FlagData()
        {
            Flag = Flag,
            Driver = Driver
        };

        return flagData;
    }
}