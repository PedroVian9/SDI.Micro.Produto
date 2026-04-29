using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SDI.Back.Template.Configuration;
using SDI.Back.Template.Models.Messaging;
using System.Text.Json;

namespace SDI.Back.Template.Messaging;

public sealed class KafkaEventPublisher : IKafkaEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly KafkaOptions options;
    private readonly ILogger<KafkaEventPublisher> logger;
    private readonly IProducer<string, string>? producer;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        this.options = options.Value;
        this.logger = logger;

        if (!this.options.Enabled)
        {
            return;
        }

        var config = new ProducerConfig
        {
            BootstrapServers = this.options.BootstrapServers,
            ClientId = this.options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = this.options.PublishTimeoutMs
        };

        producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<TPayload>(IntegrationEvent<TPayload> integrationEvent, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            logger.LogDebug("Publicacao Kafka ignorada porque Kafka esta desabilitado. EventType: {EventType}", integrationEvent.EventType);
            return;
        }

        if (producer is null)
        {
            logger.LogWarning("Publicacao Kafka ignorada porque o producer nao foi inicializado. EventType: {EventType}", integrationEvent.EventType);
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(integrationEvent, JsonOptions);
            var result = await producer.ProduceAsync(
                options.EventsTopic,
                new Message<string, string>
                {
                    Key = integrationEvent.AggregateId.ToString(),
                    Value = payload
                },
                cancellationToken);

            logger.LogInformation(
                "Evento Kafka publicado. Topic: {Topic} Partition: {Partition} Offset: {Offset} EventType: {EventType} AggregateId: {AggregateId}",
                result.Topic,
                result.Partition.Value,
                result.Offset.Value,
                integrationEvent.EventType,
                integrationEvent.AggregateId);
        }
        catch (Exception ex) when (!options.FailOnPublishError)
        {
            logger.LogError(
                ex,
                "Falha ao publicar evento Kafka. A requisicao sera mantida porque FailOnPublishError=false. EventType: {EventType} AggregateId: {AggregateId}",
                integrationEvent.EventType,
                integrationEvent.AggregateId);
        }
    }

    public void Dispose()
    {
        producer?.Flush(TimeSpan.FromSeconds(5));
        producer?.Dispose();
    }
}
