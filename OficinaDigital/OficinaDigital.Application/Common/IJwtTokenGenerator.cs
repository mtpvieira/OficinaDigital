namespace OficinaDigital.Application.Common;

public interface IJwtTokenGenerator
{
    string GerarToken(string usuario, IEnumerable<string> roles);
}
