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
    public int Latency { get; set; } = 35;

    /// <summary>
    /// The related <see cref="Event"/> of the Championship.
    /// </summary>
    public ICollection<Event> Events { get; } = new List<Event>();
}