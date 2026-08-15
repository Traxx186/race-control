using RaceControl.Data.Enums;

namespace RaceControl.Data.Events;

/// <summary>
/// An event that occurs when the flag of a session has changed
/// </summary>
public class FlagChangedEventArgs : EventArgs
{
    /// <summary>
    /// The new active flag in a session.
    /// </summary>
    public required Flag Flag { get; init; }

    /// <summary>
    /// The number of the driver for whom the flag is intended.
    /// </summary>
    public int? Driver { get; init; }
}