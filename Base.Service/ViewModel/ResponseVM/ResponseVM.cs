using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.ViewModel.ResponseVM;

public class ServiceResponseVM<T> where T : class
{
    public bool IsSuccess { get; set; }
    public string? Title { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    //public bool? IsRestored { get; set; } = false;

    public T? Result { get; set; }
}

public class ServiceResponseVM
{
    public bool IsSuccess { get; set; }
    public string? Title { get; set; }
    public IEnumerable<string>? Errors { get; set; }
}

public class AuthenticateResponseVM
{
    public string? Token { get; set; }
    public UserInformationResponseVM? Result { get; set; }
}
public class UserInformationResponseVM : Auditable
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public string? FilePath { get; set; }
    public string? DisplayName { get; set; }
    public IEnumerable<RoleResponseVM> Roles { get; set; } = new List<RoleResponseVM>();
}

public class RoleResponseVM : Auditable
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class ItemResponseVM
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool? PickUp { get; set; }
    public KeyValuePair<string, string>? ImageUrl { get; set; }
}

public class LocationResponseVM
{
    public int Id { get; set; }
    public string? Address { get; set; }
    public float Longitude { get; set; }
    public float Latitude { get; set; }
    public int Order { get; set; }
}

public class TripResponseVM
{
    public int Id { get; set; }
    public string? RoutingPreference { get; set; }
    public bool? AvoidHighways { get; set; } = false;
    public bool? AvoidTolls { get; set; } = false;
    public bool? AvoidFerries { get; set; } = false;
    public string? TravelMode { get; set; }
    public int Status { get; set; }
    public DateTime StartDate { get; set; }
    public UserInformationResponseVM? User { get; set; }
    public IEnumerable<LocationResponseVM>? Locations { get; set; }
}

public abstract class Auditable
{
    public string CreatedBy { get; set; } = "Undefined";
    public DateTime CreatedAt { get; set; }
}
