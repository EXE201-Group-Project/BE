﻿using Base.Repository.Entity;
using Base.Repository.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<Item> Items { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.HasMany(u => u.Roles).WithMany(r => r.Users)
                .UsingEntity<IdentityUserRole<Guid>>();

            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasIndex(u => u.PhoneNumber).IsUnique();
        });

        builder.Ignore<IdentityUserClaim<Guid>>();
        builder.Ignore<IdentityUserLogin<Guid>>();
        builder.Ignore<IdentityUserToken<Guid>>();
        builder.Ignore<IdentityRoleClaim<Guid>>();
    }
}
