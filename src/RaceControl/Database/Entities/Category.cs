using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceControl.Database.Entities;

[Table("category")]
public class Category
{
    public required string Name { get; set; }
    public short Priority { get; set; }
    
    [Key]
    public required string Key { get; set; }

    public required List<Session> Sessions { get; set; }
}