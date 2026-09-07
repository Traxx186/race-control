using RaceControl.Data.Enums;

namespace RaceControl.Server.Services;

public interface ITrackStatusService
{
    /// <summary>
    /// The current active flag of the session.
    /// </summary>
    Flag ActiveFlag { get; }

    /// <summary>
    /// Sets the current active flag. If the priority of the given flag equals 0, the OnFlagChange event will be called
    /// but the flag data will not be saved.
    /// </summary>
    /// <param name="flag">Flag data to be processed.</param>
    /// <param name="driver">The number of the driver for whom the flag is intended.</param>
    Task SetActiveFlagAsync(Flag flag, int? driver = null);
}