using SDI.Back.API.Exceptions;
using SDI.Back.API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SDI.Back.API.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public Guid? EmpresaId
    {
        get
        {
            var raw = Principal?.FindFirst("empresa_id")?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                            ?? Principal?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Nome => Principal?.FindFirst("name")?.Value
                           ?? Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public Guid RequireUserId()
    {
        return UserId ?? throw new DomainException("Usuario nao autenticado.", StatusCodes.Status401Unauthorized);
    }
}
