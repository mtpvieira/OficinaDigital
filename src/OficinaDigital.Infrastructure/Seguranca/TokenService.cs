using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OficinaDigital.Application.Seguranca;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Identidade;

namespace OficinaDigital.Infrastructure.Seguranca;

public class TokenService(IOptions<JwtOptions> opcoes) : ITokenService
{
    public const string ClaimClienteId = "cliente_id";

    private readonly JwtOptions _opcoes = opcoes.Value;

    public TokenResponse GerarParaUsuario(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.Valor.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Name, usuario.Nome),
            new(ClaimTypes.NameIdentifier, usuario.Id.Valor.ToString()),
            new(ClaimTypes.Role, usuario.Perfil.ToString())
        };

        return Gerar(claims, _opcoes.ExpiracaoEmMinutos, usuario.Perfil.ToString(), usuario.Nome);
    }

    public TokenResponse GerarParaCliente(ClienteId clienteId, string nome)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clienteId.Valor.ToString()),
            new(JwtRegisteredClaimNames.Name, nome),
            new(ClaimClienteId, clienteId.Valor.ToString()),
            new(ClaimTypes.Role, PerfilAcesso.Cliente)
        };

        return Gerar(claims, _opcoes.ExpiracaoClienteEmMinutos, PerfilAcesso.Cliente, nome);
    }

    private TokenResponse Gerar(IEnumerable<Claim> claims, int expiracaoEmMinutos, string perfil, string nome)
    {
        _opcoes.Validar();

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.ChaveSecreta));
        var expiraEm = DateTime.UtcNow.AddMinutes(expiracaoEmMinutos);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _opcoes.Emissor,
            Audience = _opcoes.Audiencia,
            Subject = new ClaimsIdentity(claims),
            Expires = expiraEm,
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);

        return new TokenResponse(token, "Bearer", expiracaoEmMinutos * 60, perfil, nome);
    }
}

public static class PerfilAcesso
{
    public const string Atendente = nameof(PerfilUsuario.ATENDENTE);
    public const string Mecanico = nameof(PerfilUsuario.MECANICO);
    public const string Administrador = nameof(PerfilUsuario.ADMINISTRADOR);
    public const string Cliente = "CLIENTE";

    public const string Internos = $"{Atendente},{Mecanico},{Administrador}";

    public const string MecanicoOuAdmin = $"{Mecanico},{Administrador}";

    public const string AtendenteOuAdmin = $"{Atendente},{Administrador}";
}
