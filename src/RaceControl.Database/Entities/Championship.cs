namespace RaceControl.Database.Entities;

public class Championship
{
    /// <summary>
    /// Unique identifier of the Championship.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The name of the Championship.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The broadcast latency of the Championship. Only used internally.
    /// </summary>
    public TimeSpan Latency { get; set; } = new(0, 0, 35);

    /// <summary>
    /// The time offset used in searching an active session for the championship.
    /// </summary>
    public TimeSpan SearchOffset { get; set; } = new(0, 5, 0);

    /// <summary>
    /// The related <see cref="Event"/> of the Championship.
    /// </summary>
    public ICollection<Event> Events { get; } = new List<Event>();
}