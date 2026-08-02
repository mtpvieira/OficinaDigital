using Microsoft.Extensions.Options;
using OficinaDigital.Application.Auth;

namespace OficinaDigital.Infrastructure.Auth;

public sealed class AdminAuthenticator(IOptions<AdminCredentialsOptions> options) : IAdminAuthenticator
{
    private readonly AdminCredentialsOptions _options = options.Value;

    public bool ValidarCredenciais(string usuario, string senha) =>
        string.Equals(usuario, _options.Usuario, StringComparison.Ordinal) &&
        string.Equals(senha, _options.Senha, StringComparison.Ordinal);
}
