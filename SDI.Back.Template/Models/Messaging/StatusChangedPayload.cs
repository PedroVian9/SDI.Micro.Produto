namespace SDI.Back.Template.Models.Messaging;

public sealed class StatusChangedPayload
{
    public Guid Id { get; init; }
    public bool Ativo { get; init; }
}
