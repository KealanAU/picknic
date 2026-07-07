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
    public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Postgres 'timestamptz' only accepts UTC (zero-offset) DateTimeOffsets;
        // normalise any client-supplied offset to UTC on write. SQLite (the
        // integration-test provider) can't order or compare DateTimeOffset text
        // columns, so there the values are stored as UTC ticks instead.
        var sqlite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        ValueConverter dateConverter = sqlite
            ? new ValueConverter<DateTimeOffset, long>(
                v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero))
            : new ValueConverter<DateTimeOffset, DateTimeOffset>(
                v => v.ToUniversalTime(), v => v);
        ValueConverter nullableDateConverter = sqlite
            ? new ValueConverter<DateTimeOffset?, long?>(
                v => v == null ? null : v.Value.UtcTicks,
                v => v == null ? null : new DateTimeOffset(v.Value, TimeSpan.Zero))
            : new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
                v => v == null ? null : v.Value.ToUniversalTime(), v => v);

        foreach (var prop in builder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (prop.ClrType == typeof(DateTimeOffset)) prop.SetValueConverter(dateConverter);
            else if (prop.ClrType == typeof(DateTimeOffset?)) prop.SetValueConverter(nullableDateConverter);
        }

        builder.Entity<Event>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(Event.NameMaxLength);
            e.HasMany(x => x.Photos)
                .WithOne()
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Photo>(p =>
        {
            // Makes photo registration idempotent — the Event Grid handler and the
            // client callback can both fire for one upload without duplicating it.
            p.HasIndex(x => x.BlobPath).IsUnique();
        });

        builder.Entity<Guest>(g =>
        {
            g.Property(x => x.DisplayName).HasMaxLength(Guest.DisplayNameMaxLength);
            g.HasIndex(x => x.EventId);
        });

        builder.Entity<Invite>(i =>
        {
            i.Property(x => x.Email).HasMaxLength(Invite.EmailMaxLength);
            i.HasIndex(x => new { x.EventId, x.Email }).IsUnique();
        });

        builder.Entity<ProcessedStripeEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(255);
        });
    }
}
