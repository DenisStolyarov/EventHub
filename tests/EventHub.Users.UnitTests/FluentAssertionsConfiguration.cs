using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(EventHub.Users.UnitTests.FluentAssertionsConfiguration),
    nameof(EventHub.Users.UnitTests.FluentAssertionsConfiguration.Initialize))]

namespace EventHub.Users.UnitTests;

internal static class FluentAssertionsConfiguration
{
    public static void Initialize() =>
        AssertionConfiguration.Current.Equivalency.Modify(options => options.ExcludingMissingMembers());
}
