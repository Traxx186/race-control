namespace RaceControl.Database.Entities;

public class Session
{
    public int Id { get; set; }
    public string Event { get; set; }
    public string Name { get; set; }
    public int Key { get; set; }
    public string Type { get; set; }
    public DateTime StartTime { get; set; }
    public bool Cancelled { get; set; }
    public string ChampionshipId { get; set; }

    public Championship Championship { get; set; }
}