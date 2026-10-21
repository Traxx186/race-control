using System.ComponentModel.DataAnnotations;

namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing relevant championship data.
/// </summary>
public sealed class ChampionshipDto()
{
    /// <summary>
    /// The identifier of the Championship.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// The display name of the Championship.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Events linked to the Championship.
    /// </summary>
    public ICollection<EventDto> Events { get; set; } = new List<EventDto>();
}