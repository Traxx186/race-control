using System.Text.Json.Serialization;

namespace RaceControl.Database.Entities;

public class Championship
{
    /// <summary>
    /// Unique identifier of the championship.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The name of the championship.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The broadcast latency of the championship. Only used internally.
    /// </summary>
    public int Latency { get; set; } = 35;

    /// <summary>
    /// The related <see cref="Event"/> of the championship.
    /// </summary>
    [JsonIgnore]
    public ICollection<Event> Events { get; } = new List<Event>();
}