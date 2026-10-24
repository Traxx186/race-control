namespace RaceControl.Data.Dtos.LiveTimingDtos;

/// <summary>
/// Structure of a TrackStatus method.
/// </summary>
public sealed class TrackStatusMessageDto : ILiveTimingDto
{
    /// <summary>
    /// The code of the current active flag on track.
    /// </summary>
    public string Status { get; set; }


    public string Message {  get; set; }

    /// <summary>
    /// Returns the same data as the <see cref="Status"/> propery, used in F2 and F3.
    /// </summary>
    public string Value { get; set; }
}