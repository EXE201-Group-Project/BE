using Base.Repository.Common;
using System.ComponentModel.DataAnnotations;

namespace Base.Repository.Entity;

public class Route : AuditableEntity
{
    [Key]
    public int Id { get; set; }
    public int DistanceMeters { get; set; }
    public int DurationSeconds { get; set; }
    [Required]
    public string EncodedPolylne { get; set; } = "";
    public int Order { get; set; }

    public Trip? Trip { get; set; }
    public int TripId { get; set; }

    public Location? StartPoint { get; set; }
    public int StartPointId { get; set; }

    public Location? EndPoint { get; set; }
    public int EndPointId { get; set; }
}
