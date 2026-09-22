using EventHub.Events.IntegrationTests.Fixtures;

namespace EventHub.Events.IntegrationTests.Abstractions;

public abstract class RepositoryTestBase(IntegrationTestFixture fixture) : IAsyncLifetime
{
    protected IntegrationTestFixture Fixture { get; } = fixture;

    public async ValueTask InitializeAsync() => await Fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
