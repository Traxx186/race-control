using System.ComponentModel.DataAnnotations;
using RaceControl.Database.Entities;

namespace RaceControl.Data.Dtos;

/// <summary>
/// Structure of the content of a single RaceControlMessages message.
/// </summary>
public sealed class RaceControlMessageDto
{
    /// <summary>
    /// The identifier of the Event.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Timestamp when the message was sent.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Category of the message.
    /// </summary>
    public MessageCategory Category { get; set; }

    /// <summary>
    /// The send message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Flag displayed on track.
    /// </summary>
    public string? Flag { get; set; }

    /// <summary>
    /// Scope of the flag.
    /// </summary>
    public MessageScope? Scope { get; set; }

    /// <summary>
    /// The driver racing number associated to the message.
    /// </summary>
    public int? DriverNumber { get; set; }

    /// <summary>
    /// On which lap the message was sent, only used in race sessions.
    /// </summary>
    public int? Lap { get; set; }

    /// <summary>
    /// In which mini sector the send flag is displayed.
    /// </summary>
    public int? Sector { get; set; }

    /// <summary>
    /// The parent <see cref="SessionDto"/>.
    /// </summary>
    public SessionDto Session { get; set; }
}