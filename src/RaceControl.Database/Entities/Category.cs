using System.Text.Json.Serialization;

namespace RaceControl.Database.Entities;

public class Category
{
    public string Name { get; set; }
    public short Priority { get; set; }
    public string Key { get; set; }
    public int Latency { get; set; }
    
    [JsonIgnore]
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}