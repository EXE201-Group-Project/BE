using Base.Repository.Common;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Base.Repository.Entity;

public class Item : AuditableEntity
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "Undefined";
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool PickUp { get; set; } = false;
    public KeyValuePair<string, string>? ImageUrl { get; set; }
    public bool Deleted { get; set; } = false;
    [NotMapped]
    public IFormFile? Image { get; set; }

    public Location? Location { get; set; }
    public int LocationId { get; set; }
}
