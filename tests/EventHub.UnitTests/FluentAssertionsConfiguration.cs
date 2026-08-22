using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(EventHub.UnitTests.FluentAssertionsConfiguration),
    nameof(EventHub.UnitTests.FluentAssertionsConfiguration.Initialize))]

namespace EventHub.UnitTests;

internal static class FluentAssertionsConfiguration
{
    public static void Initialize() =>
        AssertionConfiguration.Current.Equivalency.Modify(options => options.ExcludingMissingMembers());
}