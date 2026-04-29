namespace SDI.Back.Template.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; init; } = true;
    public bool FailOnPublishError { get; init; }
    public string BootstrapServers { get; init; } = "localhost:9092";
    public string ClientId { get; init; } = "sdi-micro-produto";
    public string EventsTopic { get; init; } = "sdi.produto.events";
    public int PublishTimeoutMs { get; init; } = 5000;
}
