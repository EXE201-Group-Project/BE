using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Base.Service.ViewModel.RequestVM.Trip;

public class TripVM
{
    public string? routingPreference { get; set; } = "TRAFFIC_AWARE";
    public bool? avoidHighways { get; set; } = false;
    public bool? avoidTolls { get; set; } = false;
    public bool? avoidFerries { get; set; } = false;
    public string? TravelMode { get; set; } = "DRIVE";
    public int? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public Guid? UserId { get; set; }
    public IEnumerable<Trip_LocationVM> Locations { get; set; } = new List<Trip_LocationVM>();
    public string? JsonContent { get; set; }
}

public class Trip_LocationVM
{
    [Required]
    public string Address { get; set; } = "Undefined";
    [Required]
    public float Longitude { get; set; }
    [Required]
    public float Latitude { get; set; }
    public int? Order { get; set; }
    public IEnumerable<Trip_ItemVM> Items { get; set; } = new List<Trip_ItemVM>();
}

public class Trip_ItemVM
{
    [Required]
    public string Name { get; set; } = "Undefined";
    [Required]
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool? PickUp { get; set; } = false;
    public IFormFile? Image { get; set; }
}

public class UpdateTripVM
{
    public string routingPreference { get; set; } = "TRAFFIC_AWARE";
    public bool? avoidHighways { get; set; } = false;
    public bool? avoidTolls { get; set; } = false;
    public bool? avoidFerries { get; set; } = false;
    public string TravelMode { get; set; } = "DRIVE";
    public int? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public Guid? UserId { get; set; }
}
