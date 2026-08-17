namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing data relevant when a flag category has changed.
/// </summary>
/// <param name="Key">The key of the active category.</param>
/// <param name="Latency">The TV latency of the active category.</param>
public sealed record CategoryDto(
    string Key,
    int Latency
);