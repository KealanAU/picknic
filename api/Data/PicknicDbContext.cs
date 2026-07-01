using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Picknic.Api.Models;

namespace Picknic.Api.Data;

public class PicknicDbContext(DbContextOptions<PicknicDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Invite> Invites => Set<Invite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Postgres 'timestamptz' only accepts UTC (zero-offset) DateTimeOffsets;
        // normalise any client-supplied offset to UTC on write.
        var toUtc = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            v => v.ToUniversalTime(), v => v);
        var toUtcNullable = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            v => v == null ? null : v.Value.ToUniversalTime(), v => v);

        foreach (var prop in builder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (prop.ClrType == typeof(DateTimeOffset)) prop.SetValueConverter(toUtc);
            else if (prop.ClrType == typeof(DateTimeOffset?)) prop.SetValueConverter(toUtcNullable);
        }

        builder.Entity<Event>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(120);
            e.HasMany(x => x.Photos)
                .WithOne()
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Guest>(g =>
        {
            g.Property(x => x.DisplayName).HasMaxLength(80);
            g.HasIndex(x => x.EventId);
        });

        builder.Entity<Invite>(i =>
        {
            i.Property(x => x.Email).HasMaxLength(256);
            i.HasIndex(x => new { x.EventId, x.Email }).IsUnique();
        });
    }
}
