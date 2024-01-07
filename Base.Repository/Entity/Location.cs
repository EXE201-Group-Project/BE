using Base.Repository.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repository.Entity;

public class Location : AuditableEntity
{
    [Key]
    public int Id { get; set; }
    public string? Address { get; set; }
    public int Order { get; set; }

    public Trip? Trip { get; set; }
    public int TripId { get; set; }

    public IEnumerable<Item> Items { get; set; } = new List<Item>();
}
