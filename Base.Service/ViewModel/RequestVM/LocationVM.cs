using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.ViewModel.RequestVM.Location;

public class LocationVM
{
    [Required]
    public string Address { get; set; } = "Undefined";
    [Required]
    public float Longitude { get; set; }
    [Required]
    public float Latitude { get; set; }
    public int? Order { get; set; }
    public int TripId { get; set; }
    public IEnumerable<Location_ItemVM> Items { get; set; } = new List<Location_ItemVM>();
}

public class Location_ItemVM
{
    [Required]
    public string Name { get; set; } = "Undefined";
    [Required]
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool? PickUp { get; set; } = false;
    public IFormFile? Image { get; set; }
}
