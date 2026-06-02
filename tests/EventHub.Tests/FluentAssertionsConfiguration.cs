using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(EventHub.Tests.FluentAssertionsConfiguration),
    nameof(EventHub.Tests.FluentAssertionsConfiguration.Initialize))]

namespace EventHub.Tests;

public static class FluentAssertionsConfiguration
{
    public static void Initialize() =>
        AssertionConfiguration.Current.Equivalency.Modify(options => options.ExcludingMissingMembers());
}