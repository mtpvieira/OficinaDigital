using System.Security.Claims;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Common;
using OficinaDigital.Infrastructure.Seguranca;

namespace OficinaDigital.Api.Configuracao;

public class UsuarioAtual(IHttpContextAccessor acessor) : IUsuarioAtual
{
    private ClaimsPrincipal? Principal => acessor.HttpContext?.User;

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated == true;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? Perfil => Principal?.FindFirstValue(ClaimTypes.Role);

    public UsuarioId? Id =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? new UsuarioId(id)
            : null;

    public ClienteId? ClienteId =>
        Guid.TryParse(Principal?.FindFirstValue(TokenService.ClaimClienteId), out var id)
            ? new ClienteId(id)
            : null;
}
