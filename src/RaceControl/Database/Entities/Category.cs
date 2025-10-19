namespace RaceControl.Database.Entities;

public class Category
{
    public required string Name { get; init; }
    public short Priority { get; init; }
    public required string Key { get; init; }
    public required int Latency { get; init; }
    public required ICollection<Session> Sessions { get; init; } = new List<Session>();
}