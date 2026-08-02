namespace OficinaDigital.Application.Common;

public sealed record TokenGerado(string Token, DateTime ExpiraEm);

public interface IJwtTokenGenerator
{
    TokenGerado GerarToken(string usuario, IEnumerable<string> roles);
}
