namespace SDI.Back.API.Models.Messaging;

public sealed class StatusChangedPayload
{
    public Guid Id { get; init; }
    public bool Ativo { get; init; }
}
