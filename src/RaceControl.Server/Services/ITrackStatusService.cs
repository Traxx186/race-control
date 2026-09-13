using RaceControl.Data.Dtos;
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
    /// <param name="flagData">Flag data to be processed.</param>
    Task SetActiveFlagAsync(FlagDataDto flagData);
}