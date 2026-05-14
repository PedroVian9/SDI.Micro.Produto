namespace SDI.Back.API.Services.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? EmpresaId { get; }
    string? Email { get; }
    string? Nome { get; }
    Guid RequireUserId();
}
