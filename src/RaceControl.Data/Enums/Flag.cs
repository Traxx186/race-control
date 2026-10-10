using System.Text.Json.Serialization;

namespace RaceControl.Data.Enums;

/// <summary>
/// Possible flags that can be displayed on the flag panels.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Flag>))]
public enum Flag
{
    BlackWhite,
    Blue,
    Chequered,
    Clear,
    Code60,
    DoubleYellow,
    Fyc,
    Red,
    RollingStart,
    SafetyCar,
    StandingStart,
    Surface,
    Vsc,
    Yellow,
    None
}