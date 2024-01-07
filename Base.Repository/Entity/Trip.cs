using Base.Repository.Common;
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

    public int Status { get; set; }

    public IEnumerable<Location> Locations { get; set; } = new List<Location>();
}
