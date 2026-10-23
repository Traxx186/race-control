namespace RaceControl.Data.Dtos.Signalr;

/// <summary>
/// A DTO representing relevant championship data to be sent with SignalR.
/// </summary>
public class ChampionshipSignalrDto
{
    /// <summary>
    /// The identifier of the Championship.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The display name of the Championship.
    /// </summary>
    public required string Name { get; set; }
}