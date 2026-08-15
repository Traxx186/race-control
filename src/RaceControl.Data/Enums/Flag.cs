using System.Text.Json.Serialization;

namespace RaceControl.Data.Enums;

/// <summary>
/// Possible flags that can be displayed on the flag panels.
/// </summary>
[Flags]
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
    SafetyCar,
    Surface,
    Vsc,
    Yellow,
    None
}