using Base.Repository.Common;
using System.ComponentModel.DataAnnotations;

namespace Base.Repository.Entity;

public class Location : AuditableEntity
{
    [Key]
    public int Id { get; set; }
    public string Address { get; set; } = "Undefined";
    public float Longitude { get; set; }
    public float Latitude { get; set; }
    public int Order { get; set; }
    public bool Deleted { get; set; } = false;

    public Trip? Trip { get; set; }
    public int TripId { get; set; }

    public Route? StartPointRoute { get; set; }

    public Route? EndPointRoute { get; set; }

    public IEnumerable<Item> Items { get; set; } = new List<Item>();
}
