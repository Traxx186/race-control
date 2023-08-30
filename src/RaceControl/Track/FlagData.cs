using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RaceControl.Track;

/// <summary>
/// Object that contains a flag and the related driver.
/// </summary>
public record FlagData
{
    /// <summary>
    /// The <see cref="Flag"/>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public Flag Flag;
    
    /// <summary>
    /// The driver number that related to the flag
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? Driver;
}