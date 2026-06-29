namespace EventHub.Tests.TestUtilities;

public static class TestDateTime
{
    public static DateTime UtcDateTime(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    public static DateTimeOffset UtcDate(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);
}
