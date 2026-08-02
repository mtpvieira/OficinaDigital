using FluentValidation;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Common.Exceptions;

namespace OficinaDigital.Application.Auth;

public sealed class AuthService(
    IAdminAuthenticator adminAuthenticator,
    IJwtTokenGenerator jwtTokenGenerator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    private static readonly IReadOnlyList<string> RolesAdmin = ["Admin"];

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!adminAuthenticator.ValidarCredenciais(request.Usuario, request.Senha))
            throw new InvalidCredentialsException("Usuário ou senha inválidos.");

        var token = jwtTokenGenerator.GerarToken(request.Usuario, RolesAdmin);

        return new LoginResponse(token.Token, token.ExpiraEm);
    }
}
