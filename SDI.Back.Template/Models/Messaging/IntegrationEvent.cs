namespace SDI.Back.Template.Models.Messaging;

public sealed class IntegrationEvent<TPayload>
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public string Source { get; init; } = "SDI.Micro.Produto";
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string AggregateType { get; init; } = string.Empty;
    public Guid AggregateId { get; init; }
    public Guid? UserId { get; init; }
    public TPayload Payload { get; init; } = default!;
}
