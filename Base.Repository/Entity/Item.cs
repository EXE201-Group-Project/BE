using Base.Repository.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repository.Entity;

public class Item : AuditableEntity
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "Undefined";
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool Deleted { get; set; } = false;

    public Location? Location { get; set; }
    public int LocationId { get; set; }
}
