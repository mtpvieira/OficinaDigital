using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Api.Configuracao;
using OficinaDigital.Application.Seguranca;

namespace OficinaDigital.Api.Controllers;

/// <summary>Autenticação das APIs administrativas e do app do cliente.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Login do usuário interno (atendente, mecânico ou administrador).</summary>
    /// <response code="200">Token emitido.</response>
    /// <response code="403">Credenciais inválidas.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    /// <summary>Login do cliente pelo app, com CPF/CNPJ e a placa de um veículo dele.</summary>
    [HttpPost("login-cliente")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TokenResponse>> LoginCliente(LoginClienteRequest request,
        CancellationToken ct) =>
        Ok(await auth.LoginClienteAsync(request, ct));

    /// <summary>Cria um usuário interno. Restrito ao perfil ADMINISTRADOR.</summary>
    [HttpPost("usuarios")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> CriarUsuario(CriarUsuarioRequest request,
        CancellationToken ct)
    {
        var usuario = await auth.CriarUsuarioAsync(request, ct);
        return CreatedAtAction(nameof(ListarUsuarios), new { }, usuario);
    }

    /// <summary>Lista os usuários internos. Restrito ao perfil ADMINISTRADOR.</summary>
    [HttpGet("usuarios")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<IReadOnlyList<UsuarioResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> ListarUsuarios(CancellationToken ct) =>
        Ok(await auth.ListarUsuariosAsync(ct));
}
