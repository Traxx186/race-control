using System.Text.Json.Nodes;

namespace RaceControl.Data.Dtos.LiveTimingDtos;

/// <summary>
/// Structure of a RaceControlMessages method.
/// </summary>
public sealed record RaceControlMessagesDto(
    JsonNode Messages
);

/// <summary>
/// Structure of the content of a single RaceControlMessages message.
/// </summary>
public sealed record RaceControlMessageDto(
    string Category,
    string Message,
    string Flag,
    string RacingNumber
);