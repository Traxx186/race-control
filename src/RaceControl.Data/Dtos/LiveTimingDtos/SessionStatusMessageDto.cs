namespace RaceControl.Data.Dtos.LiveTimingDtos;

/// <summary>
/// Structure of a SessionStatus method message.
/// </summary>
public sealed class SessionStatusMessageDto : ILiveTimingDto
{
    /// <summary>
    /// Returns what the status is of the session i.e. started, finished or finalized
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Returns the same data as the <see cref="Status"/> propery, used in F2 and F3.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// If the session has started or is finished
    /// </summary>
    public string? Started { get; set; }
}