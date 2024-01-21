using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.ViewModel.RequestVM.Item;

public class ItemVM
{
    [Required]
    public string Name { get; set; } = "Undefined";
    [Required]
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool? PickUp { get; set; } = false;
    public IFormFile? Image { get; set; }
    public int? LocationId { get; set; }
}

public class UpdateItemVM
{
    public string? Name { get; set; }
    public int? Quantity { get; set; }
    public string? Description { get; set; }
    public bool? PickUp { get; set; } = false;
    public IFormFile? Image { get; set; }
    public int? LocationId { get; set; }
}
