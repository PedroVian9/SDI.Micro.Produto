using SDI.Back.API.Models.Messaging;

namespace SDI.Back.API.Messaging;

public interface IKafkaEventPublisher
{
    Task PublishAsync<TPayload>(IntegrationEvent<TPayload> integrationEvent, CancellationToken cancellationToken);
}
