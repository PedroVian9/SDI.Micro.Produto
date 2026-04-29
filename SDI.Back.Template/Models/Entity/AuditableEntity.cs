namespace SDI.Back.Template.Models.Entity;

public abstract class AuditableEntity
{
    public Guid Id { get; init; }
    public bool Ativo { get; init; }
    public DateTimeOffset DataCadastro { get; init; }
    public Guid? UsuarioCadastro { get; init; }
    public DateTimeOffset? UltimaAlteracao { get; init; }
    public Guid? UsuarioAlteracao { get; init; }
}
