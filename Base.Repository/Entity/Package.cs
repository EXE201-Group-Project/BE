using Base.Repository.Common;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repository.Entity;

public class Package : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "money")]
    public Decimal Price { get; set; } = 0;

    public bool IsDeleted { get; set; } = false;
    public bool IsValid { get; set; } = true;

    public IEnumerable<Bill> Bills { get; set; } = new List<Bill>();
}
