using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Infrastructure.BackgroundServices;

public sealed class BookingProcessor(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingProcessor> logger) : BackgroundService
{
    private const int ProcessingDelaySeconds = 2;
    private const int PollingIntervalSeconds = 5;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking processor is started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IEnumerable<Booking> pendingBookings = bookingRepository.GetPendingBookings();
                IEnumerable<Task> tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));

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

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("Booking processor is stopped");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        logger.LogInformation("Processing booking {id}", booking.Id);

        await Task.Delay(TimeSpan.FromSeconds(ProcessingDelaySeconds), stoppingToken);

        await _processingSemaphore.WaitAsync(stoppingToken);

        Event? @event = null;

        try
        {
            @event = eventRepository.GetById(booking.EventId);

            if (@event is null)
            {
                booking.Reject();

                logger.LogWarning("Event {id} not found for booking {bookingId}", booking.EventId, booking.Id);
            }
            else
            {
                booking.Confirm();
            }

            bookingRepository.Update(booking);

            logger.LogInformation("Booking {id} is processed", booking.Id);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process booking {id}", booking.Id);

            HandleProcessingFailure(booking, @event);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    private void HandleProcessingFailure(Booking booking, Event? @event)
    {
        try
        {
            if (@event is not null)
            {
                @event.ReleaseSeats();
                eventRepository.Update(@event);
            }

            booking.Reject();
            bookingRepository.Update(booking);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to recover booking {id}", booking.Id);
        }
    }
}
