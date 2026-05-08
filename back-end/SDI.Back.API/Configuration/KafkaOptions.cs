namespace SDI.Back.API.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; } = true;
    public bool FailOnPublishError { get; set; }
    public string BootstrapServers { get; set; } = "redpanda:9092";
    public string ClientId { get; set; } = "produtos-service";
    public int PublishTimeoutMs { get; set; } = 5000;
    public List<string> AllowedEventTypes { get; set; } = [SDI.Back.API.Messaging.EventTypes.ProdutoCriado];
}
