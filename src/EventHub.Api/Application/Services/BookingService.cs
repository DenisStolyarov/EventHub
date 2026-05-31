using EventHub.Api.Application.Dto.Bookings;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Interfaces;

namespace EventHub.Api.Application.Services;

public class BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository) : IBookingService
{
    public Task<BookingInfo> CreateBookingAsync(Guid eventId)
    {
        Event @event = eventRepository.GetById(eventId)
            ?? throw new NotFoundException(nameof(Event), eventId);

        Booking booking = new(Guid.CreateVersion7(), @event.Id);

        bookingRepository.Add(booking);

        return Task.FromResult(booking.ToInfo());
    }

    public Task<BookingInfo> GetBookingByIdAsync(Guid bookingId)
    {
        Booking booking = bookingRepository.GetById(bookingId)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        return Task.FromResult(booking.ToInfo());
    }
}