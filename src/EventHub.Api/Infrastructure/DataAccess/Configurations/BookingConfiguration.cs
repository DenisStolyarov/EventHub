using EventHub.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Api.Infrastructure.DataAccess.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder
            .Property(b => b.Id)
            .ValueGeneratedNever();

        builder
            .Property(b => b.CreatedAt)
            .IsRequired();

        builder
            .Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>();

        builder
            .HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId);
    }
}