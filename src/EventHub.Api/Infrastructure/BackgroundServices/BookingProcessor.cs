using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Infrastructure.BackgroundServices;

public class BookingProcessor(IServiceScopeFactory scopeFactory, ILogger<BookingProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking processor is started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                IBookingRepository bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                IEnumerable<Booking> bookings = bookingRepository.GetPendingBookings();

                foreach (Booking booking in bookings)
                {
                    logger.LogInformation("Processing booking {id}", booking.Id);

                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                    booking.Confirm();

                    bookingRepository.Update(booking);

                    logger.LogInformation("Booking {id} is processed", booking.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        logger.LogInformation("Booking processor is stopped");
    }
}