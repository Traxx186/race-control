using System.Text.Json.Serialization;
using RaceControl.Data.Enums;

namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing data relevant when a flag changed.
/// </summary>
/// <param name="Flag">The new active flag in a session.</param>
/// <param name="Driver">The number of the driver for whom the flag is intended.</param>
public record FlagDataDto(
    Flag Flag,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Driver = null
);