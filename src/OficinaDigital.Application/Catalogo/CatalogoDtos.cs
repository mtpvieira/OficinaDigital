using System.ComponentModel.DataAnnotations;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Application.Catalogo;

public sealed record CadastrarServicoRequest(
    [Required, StringLength(120, MinimumLength = 3)] string Nome,
    [StringLength(500)] string? Descricao,
    [Range(0.01, 1_000_000)] decimal Preco,
    [Range(1, 100_000)] int TempoPadraoMinutos);

public sealed record AlterarServicoRequest(
    [Required, StringLength(120, MinimumLength = 3)] string Nome,
    [StringLength(500)] string? Descricao,
    [Range(1, 100_000)] int TempoPadraoMinutos);

public sealed record AlterarPrecoRequest(
    [Range(0.01, 1_000_000)] decimal Preco,
    DateOnly? VigenteDe);

public sealed record PrecoVigenteDto(decimal Valor, DateOnly VigenteDe);

public sealed record ServicoResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal PrecoAtual,
    int TempoPadraoMinutos,
    bool Ativo,
    IReadOnlyList<PrecoVigenteDto> HistoricoDePrecos)
{
    public static ServicoResponse De(Servico s, DateOnly hoje) => new(
        s.Id.Valor,
        s.Nome,
        s.Descricao,
        s.PrecoVigenteEm(hoje).Valor,
        s.TempoPadraoExecucao.Minutos,
        s.Ativo,
        s.Precos.OrderBy(p => p.VigenteDe).Select(p => new PrecoVigenteDto(p.Valor.Valor, p.VigenteDe)).ToList());
}

public sealed record CadastrarPecaRequest(
    [Required, StringLength(30, MinimumLength = 3)] string Sku,
    [Required, StringLength(200, MinimumLength = 3)] string Descricao,
    [Required] UnidadeDeMedida Unidade,
    [Range(0.01, 1_000_000)] decimal PrecoVenda,
    [Range(0, 1_000_000)] decimal EstoqueMinimo);

public sealed record AlterarPecaRequest([Required, StringLength(200, MinimumLength = 3)] string Descricao);

public sealed record PecaResponse(
    Guid Id,
    string Sku,
    string Descricao,
    UnidadeDeMedida Unidade,
    decimal PrecoAtual,
    bool Ativa,
    IReadOnlyList<PrecoVigenteDto> HistoricoDePrecos)
{
    public static PecaResponse De(Peca p, DateOnly hoje) => new(
        p.Id.Valor,
        p.Sku.Codigo,
        p.Descricao,
        p.Unidade,
        p.PrecoVigenteEm(hoje).Valor,
        p.Ativa,
        p.Precos.OrderBy(x => x.VigenteDe).Select(x => new PrecoVigenteDto(x.Valor.Valor, x.VigenteDe)).ToList());
}
