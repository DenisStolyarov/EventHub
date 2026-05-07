namespace EventHub.Api.Domain.ValueObjects;

public readonly record struct Period
{
    public DateTime StartAt { get; }

    public DateTime EndAt { get; }

    public Period(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new ArgumentException("EndAt must be later than StartAt.", nameof(EndAt));
        }

        StartAt = startAt;
        EndAt = endAt;
    }
}