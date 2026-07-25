using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Enums;
using EventHub.Api.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Infrastructure.BackgroundServices;

public sealed class BookingProcessor(IServiceScopeFactory scopeFactory, ILogger<BookingProcessor> logger) : BackgroundService
{
    private readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking processor is started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> pendingBookingIds;

                await using (AsyncServiceScope scope = scopeFactory.CreateAsyncScope())
                {
                    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    pendingBookingIds = await db.Bookings
                        .Where(booking => booking.Status == BookingStatus.Pending)
                        .Select(booking => booking.Id)
                        .ToListAsync(stoppingToken);
                }

                IEnumerable<Task> tasks = pendingBookingIds.Select(id => ProcessBookingAsync(id, stoppingToken));

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("Booking processor is stopped");
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        logger.LogInformation("Processing booking {id}", bookingId);

        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Booking? booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);

            if (booking is null)
            {
                logger.LogWarning("Booking {id} not found", bookingId);

                return;
            }

            if (booking.Status is not BookingStatus.Pending)
            {
                logger.LogWarning("Booking {id} is not pending", bookingId);

                return;
            }

            Event? @event = await db.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);

            if (@event is null)
            {
                booking.Reject();

                await db.SaveChangesAsync(stoppingToken);

                logger.LogWarning("Event {id} not found for booking {bookingId}", booking.EventId, booking.Id);

                return;
            }

            booking.Confirm();

            await db.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Booking {id} is processed", booking.Id);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Booking? booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);

                if (booking is not null)
                {
                    booking.Reject();

                    Event? @event = await db.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);

                    @event?.ReleaseSeats();

                    await db.SaveChangesAsync(stoppingToken);
                }

                logger.LogError(ex, "Booking {Id} rejected due to processing error", bookingId);
            }
            catch (Exception recEx)
            {
                logger.LogError(recEx, "Failed to reject booking {Id}", bookingId);
            }
        }
    }
}