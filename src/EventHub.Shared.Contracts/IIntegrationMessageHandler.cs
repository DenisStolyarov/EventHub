namespace EventHub.Shared.Contracts;

public interface IIntegrationMessageHandler<in TMessage>
    where TMessage : IIntegrationEvent
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
