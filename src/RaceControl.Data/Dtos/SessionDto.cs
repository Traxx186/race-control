namespace RaceControl.Data.Dtos;

public class SessionDto()
{
    /// <summary>
    /// The name of the Session.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The Session key.
    /// </summary>
    public int Key { get; set; }

    /// <summary>
    /// The Session type.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Start time of the Session in UTC.
    /// </summary>
    public string StartTime { get; set; }

    /// <summary>
    /// If the Session is canceled, is set manually.
    /// </summary>
    public bool Cancelled { get; set; }
}