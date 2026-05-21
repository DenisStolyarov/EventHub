using EventHub.Api.Domain.Exceptions;

namespace EventHub.Api.Domain.ValueObjects;

public readonly record struct Period
{
    public DateTime StartAt { get; }

    public DateTime EndAt { get; }

    public Period(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new DomainException(nameof(EndAt), "EndAt must be later than StartAt.");
        }

        StartAt = startAt;
        EndAt = endAt;
    }
}