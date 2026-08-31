using System.ComponentModel.DataAnnotations;
using OficinaDigital.Domain.Identidade;

namespace OficinaDigital.Application.Seguranca;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Senha);

public sealed record LoginClienteRequest(
    [Required, StringLength(18, MinimumLength = 11)] string Documento,
    [Required, StringLength(8, MinimumLength = 7)] string Placa);

public sealed record CriarUsuarioRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Senha,
    [Required] PerfilUsuario Perfil);

public sealed record TokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiraEmSegundos,
    string Perfil,
    string Nome);

public sealed record UsuarioResponse(Guid Id, string Nome, string Email, PerfilUsuario Perfil, bool Ativo)
{
    public static UsuarioResponse De(Usuario u) => new(u.Id.Valor, u.Nome, u.Email, u.Perfil, u.Ativo);
}
