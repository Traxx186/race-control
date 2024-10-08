namespace RaceControl.Track;

/// <summary>
/// Possible flags that can be displayed on the flag panels. Note, flags that are cannot bet fetched from APIs are not
/// present.
/// </summary>
[Flags]
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