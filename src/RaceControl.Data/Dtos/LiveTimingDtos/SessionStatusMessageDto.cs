namespace RaceControl.Data.Dtos.LiveTimingDtos;

/// <summary>
/// Structure of a SessionStatus method message.
/// </summary>
public sealed record SessionStatusMessageDto(
    string Status,
    string Value,
    string Started
);