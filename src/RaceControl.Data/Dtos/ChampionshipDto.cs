namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing data relevant when a championship has changed.
/// </summary>
/// <param name="Key">The key of the active championchip.</param>
/// <param name="Latency">The TV latency of the active category.</param>
public sealed record ChampionshipDto(
    string Key,
    int Latency
);