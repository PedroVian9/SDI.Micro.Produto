using SDI.Back.Template.Models.Messaging;

namespace SDI.Back.Template.Messaging;

public interface IKafkaEventPublisher
{
    Task PublishAsync<TPayload>(IntegrationEvent<TPayload> integrationEvent, CancellationToken cancellationToken);
}
