using Base.Repository.Common;
using Base.Repository.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repository.Entity;

public class Bill : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "money")]
    public Decimal Price { get; set; } = 0;

    public DateTime IssuedDate { get; set; }
    public DateTime ExpiredDate { get; set; }

    public bool Deleted { get; set; } = false;


    public Package? Package { get; set; }
    public int PackageId { get; set; }

    public User? User { get; set; }
    public Guid UserId { get; set; }
}
