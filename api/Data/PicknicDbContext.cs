using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Models;

namespace Picknic.Api.Data;

public class PicknicDbContext(DbContextOptions<PicknicDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
    }
}
