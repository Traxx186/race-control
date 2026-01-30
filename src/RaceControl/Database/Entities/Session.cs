namespace RaceControl.Database.Entities;

public class Session 
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public string Key { get; set; }
    
    public DateTime Time { get; set; }

    public string CategoryKey { get; set; }
    
    public Category Category { get; set; }
}

public record PatchSessionLatency(int Latency);