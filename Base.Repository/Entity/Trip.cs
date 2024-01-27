using Base.Repository.Common;
using Base.Repository.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repository.Entity;

public class Trip : AuditableEntity
{
    [Key]
    public int Id { get; set; }
    public string RoutingPreference { get; set; } = "TRAFFIC_AWARE";
    public bool? AvoidHighways { get; set; } = false;
    public bool? AvoidTolls { get; set; } = false;
    public bool? AvoidFerries { get; set; } = false;
    public string TravelMode { get; set; } = "DRIVE";
    public int Status { get; set; }
    public bool Deleted { get; set; } = false;
    public DateTime StartDate { get; set; }

    public User? User { get; set; }
    public Guid UserId { get; set; }

    public IEnumerable<Location> Locations { get; set; } = new List<Location>();
    public IEnumerable<Route> Routes { get; set; } = new List<Route>();
}



