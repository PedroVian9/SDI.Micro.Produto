using System.Text.Json.Serialization;

namespace SDI.Back.Template.Models.Messaging;

public sealed class IntegrationEvent<TPayload>
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public string EventVersion { get; init; } = "1.0";
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "produtos-service";
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public TPayload Payload { get; init; } = default!;

    [JsonIgnore]
    public string AggregateType { get; init; } = string.Empty;

    [JsonIgnore]
    public Guid AggregateId { get; init; }

    [JsonIgnore]
    public Guid? UserId { get; init; }
}
