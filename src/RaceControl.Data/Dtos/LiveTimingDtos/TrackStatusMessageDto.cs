namespace RaceControl.Data.Dtos.LiveTimingDtos;

/// <summary>
/// Structure of a TrackStatus method.
/// </summary>
public sealed record TrackStatusMessageDto(
    string Status,
    string Message,
    string Value
);