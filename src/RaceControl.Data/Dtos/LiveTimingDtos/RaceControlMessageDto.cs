namespace RaceControl.Data.Dtos.LiveTimingDtos;

/// <summary>
/// Structure of the content of a single RaceControlMessages message.
/// </summary>
public sealed record RaceControlMessageDto(
    DateTime Utc,
    string RacingNumber,
    int Lap,
    string Category,
    string Flag,
    string Scope,
    int Sector,
    string Message
);