namespace OficinaDigital.Application.Auth;

public interface IAdminAuthenticator
{
    bool ValidarCredenciais(string usuario, string senha);
}
