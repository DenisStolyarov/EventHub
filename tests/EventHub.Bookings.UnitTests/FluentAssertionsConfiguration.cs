using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(EventHub.Bookings.UnitTests.FluentAssertionsConfiguration),
    nameof(EventHub.Bookings.UnitTests.FluentAssertionsConfiguration.Initialize))]

namespace EventHub.Bookings.UnitTests;

internal static class FluentAssertionsConfiguration
{
    public static void Initialize() =>
        AssertionConfiguration.Current.Equivalency.Modify(options => options.ExcludingMissingMembers());
}
