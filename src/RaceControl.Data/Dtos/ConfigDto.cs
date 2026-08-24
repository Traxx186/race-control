namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing data relevant to update app configuration.
/// </summary>
/// <param name="Formula1AccessToken">Access token to authenticate with FOM services.</param>
public record ConfigDto(
    string Formula1AccessToken
);