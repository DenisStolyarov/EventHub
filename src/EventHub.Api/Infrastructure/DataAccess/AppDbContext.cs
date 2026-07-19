using EventHub.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Infrastructure.DataAccess;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings { get; set; }

    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
