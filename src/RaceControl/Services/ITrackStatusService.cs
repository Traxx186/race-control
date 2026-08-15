using RaceControl.Data.Enums;

namespace RaceControl.Services;

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

    /// <summary>
    /// Converts the input string to a <see cref="Flag"/>.
    /// </summary>
    /// <param name="input">The string representing a flag.</param>
    /// <param name="flag">
    /// When this method returns <see langword="true"/>, the related <see cref="Flag"/> item.
    /// Else <code>Flag.None</code> will be returned.
    /// </param>
    /// <returns>If the flag could be parsed.</returns>
    static bool TryParseFlag(string? input, out Flag flag) => throw new NotImplementedException();
}