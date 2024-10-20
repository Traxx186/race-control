using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceControl.Database.Entities;

[Table("session")]
public class Session 
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Key { get; set; }
    public DateTime Time { get; set; }

    public required string CategoryKey { get; set; }
    public required Category Category { get; set; }
}