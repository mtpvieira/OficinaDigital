using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OficinaDigital.Application.Common;

namespace OficinaDigital.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public TokenGerado GerarToken(string usuario, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var chaveAssinatura = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ChaveSecreta));
        var credenciais = new SigningCredentials(chaveAssinatura, SecurityAlgorithms.HmacSha256);
        var expiraEm = DateTime.UtcNow.AddMinutes(_options.ExpiracaoMinutos);

        var token = new JwtSecurityToken(
            issuer: _options.Emissor,
            audience: _options.Audiencia,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        var tokenSerializado = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenGerado(tokenSerializado, expiraEm);
    }
}
