using System.Text.Json.Serialization;

namespace RaceControl.Database.Entities;

public class Championship
{
    public string Id { get; set; }
    public string Name { get; set; }

    [JsonIgnore]
    public ICollection<Session> Sessions { get; } = new List<Session>();
}