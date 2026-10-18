namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing data relevant when a championship has changed.
/// </summary>
/// /// <param name="Id">The identifier of the championship.</param>
/// <param name="Name">The display name of the championship.</param>
public sealed record ChampionshipDto(
    string Id,
    string Name
);