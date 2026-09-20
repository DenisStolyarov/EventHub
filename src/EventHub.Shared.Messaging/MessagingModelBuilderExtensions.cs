using Microsoft.EntityFrameworkCore;

namespace EventHub.Shared.Messaging;

public static class MessagingModelBuilderExtensions
{
    public static ModelBuilder ApplyMessagingConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());

        return modelBuilder;
    }
}