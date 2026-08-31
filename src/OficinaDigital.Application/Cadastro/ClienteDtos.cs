using System.ComponentModel.DataAnnotations;
using OficinaDigital.Domain.Cadastro;
using OficinaDigital.Domain.Cadastro.ValueObjects;

namespace OficinaDigital.Application.Cadastro;

public sealed record ContatoDto(
    [Required] CanalContato Canal,
    [Required, StringLength(120, MinimumLength = 8)] string Valor);

public sealed record EnderecoDto(
    [Required, StringLength(150)] string Logradouro,
    [Required, StringLength(15)] string Numero,
    [StringLength(60)] string? Complemento,
    [Required, StringLength(80)] string Cidade,
    [Required, StringLength(2, MinimumLength = 2)] string Uf,
    [Required, StringLength(9, MinimumLength = 8)] string Cep);

public sealed record CadastrarClienteRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Nome,
    [Required, StringLength(18, MinimumLength = 11)] string Documento,
    [Required, MinLength(1)] IReadOnlyList<ContatoDto> Contatos,
    EnderecoDto? Endereco);

public sealed record AlterarClienteRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Nome,
    [Required, MinLength(1)] IReadOnlyList<ContatoDto> Contatos,
    EnderecoDto? Endereco);

public sealed record ClienteResponse(
    Guid Id,
    string Nome,
    string Documento,
    string DocumentoFormatado,
    TipoPessoa Tipo,
    IReadOnlyList<ContatoDto> Contatos,
    EnderecoDto? Endereco,
    bool Ativo,
    DateTime CriadoEm)
{
    public static ClienteResponse De(Cliente c) => new(
        c.Id.Valor,
        c.Nome,
        c.Documento.Numero,
        c.Documento.Formatado(),
        c.Documento.Tipo,
        c.Contatos.Select(x => new ContatoDto(x.Canal, x.Valor)).ToList(),
        c.Endereco is null
            ? null
            : new EnderecoDto(c.Endereco.Logradouro, c.Endereco.Numero, c.Endereco.Complemento,
                c.Endereco.Cidade, c.Endereco.Uf, c.Endereco.Cep),
        c.Ativo,
        c.CriadoEm);
}
