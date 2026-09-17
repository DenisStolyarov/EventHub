using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(EventHub.Events.UnitTests.FluentAssertionsConfiguration),
    nameof(EventHub.Events.UnitTests.FluentAssertionsConfiguration.Initialize))]

namespace EventHub.Events.UnitTests;

internal static class FluentAssertionsConfiguration
{
    public static void Initialize() =>
        AssertionConfiguration.Current.Equivalency.Modify(options => options.ExcludingMissingMembers());
}
