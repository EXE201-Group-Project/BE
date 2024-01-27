using Base.Repository.Entity;
using Base.Repository.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;

namespace Base.Repository.Common;

public interface IApplicationDbContext : IDisposable
{
    public DbSet<Bill> Bills { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Item> Items { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
}

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Bill> Bills { get; set; } = null!;
    public DbSet<Package> Packages { get; set; } = null!;
    public DbSet<Trip> Trips { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<Route> Routes { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Define a custom value comparer for key-value<string,string>
        var keyValueComparer = new ValueComparer<KeyValuePair<string,string>?>(
            (c1, c2) => c1.HasValue && c2.HasValue ? JsonConvert.SerializeObject(c1.Value) == JsonConvert.SerializeObject(c2.Value) : c1.HasValue == c2.HasValue,
            c => c.HasValue ? JsonConvert.SerializeObject(c.Value).GetHashCode() : 0,
            c => c.HasValue ? JsonConvert.DeserializeObject<KeyValuePair<string, string>>(JsonConvert.SerializeObject(c.Value)) : (KeyValuePair<string, string>?)null);

        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.HasMany(u => u.Roles).WithMany(r => r.Users)
                .UsingEntity<IdentityUserRole<Guid>>();

            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasIndex(u => u.PhoneNumber).IsUnique();
        });

        builder.Entity<Route>(entity =>
        {
            entity.HasOne(r => r.Origin)
                .WithOne(l => l.StartPointRoute)
                .HasForeignKey<Route>(r => r.StartPointId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Destination)
                .WithOne(l => l.EndPointRoute)
                .HasForeignKey<Route>(r => r.EndPointId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Item>(entity =>
        {
            entity.Property(i => i.ImageUrl)
                .HasConversion(
                value => JsonConvert.SerializeObject(value),
                serializedValue => JsonConvert.DeserializeObject<KeyValuePair<string, string>>(serializedValue))
                .Metadata.SetValueComparer(keyValueComparer);
        });

        builder.Ignore<IdentityUserClaim<Guid>>();
        builder.Ignore<IdentityUserLogin<Guid>>();
        builder.Ignore<IdentityUserToken<Guid>>();
        builder.Ignore<IdentityRoleClaim<Guid>>();
    }
}
