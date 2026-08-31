using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.BackgroundServices;

public sealed class BookingProcessor(IServiceScopeFactory scopeFactory, ILogger<BookingProcessor> logger, TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking processor is started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ICollection<Guid> pendingBookingIds;

                await using (AsyncServiceScope scope = scopeFactory.CreateAsyncScope())
                {
                    IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    pendingBookingIds = await unitOfWork.Bookings.GetPendingBookingIdsAsync(stoppingToken);
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
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            Booking? booking = await unitOfWork.Bookings.GetByIdAsync(bookingId, stoppingToken);

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

            Event? @event = await unitOfWork.Events.GetByIdAsync(booking.EventId, stoppingToken);

            if (@event is null)
            {
                booking.Reject(timeProvider.GetUtcNow().UtcDateTime);

                await unitOfWork.SaveChangesAsync(stoppingToken);

                logger.LogWarning("Event {id} not found for booking {bookingId}", booking.EventId, booking.Id);

                return;
            }

            booking.Confirm(timeProvider.GetUtcNow().UtcDateTime);

            await unitOfWork.SaveChangesAsync(stoppingToken);

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
                IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                Booking? booking = await unitOfWork.Bookings.GetByIdAsync(bookingId, stoppingToken);

                if (booking is not null)
                {
                    booking.Reject(timeProvider.GetUtcNow().UtcDateTime);

                    Event? @event = await unitOfWork.Events.GetByIdAsync(booking.EventId, stoppingToken);

                    @event?.ReleaseSeats();

                    await unitOfWork.SaveChangesAsync(stoppingToken);
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
