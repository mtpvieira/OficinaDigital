using System.ComponentModel.DataAnnotations;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Estoque;

namespace OficinaDigital.Application.Estoque;

public sealed record RegistrarEntradaRequest(
    [Range(0.001, 1_000_000)] decimal Quantidade,
    [Range(0.01, 1_000_000)] decimal CustoUnitario);

public sealed record DefinirEstoqueMinimoRequest([Range(0, 1_000_000)] decimal EstoqueMinimo);

public sealed record ReservaDto(Guid OrdemDeServicoId, decimal Quantidade, DateTime ReservadaEm);

public sealed record MovimentoDto(
    TipoMovimentoEstoque Tipo,
    decimal Quantidade,
    decimal SaldoResultante,
    Guid? OrdemDeServicoId,
    string? Motivo,
    DateTime RegistradoEm);

public sealed record ItemEstoqueResponse(
    Guid Id,
    Guid PecaId,
    string Sku,
    string Descricao,
    UnidadeDeMedida Unidade,
    decimal Saldo,
    decimal Reservado,
    decimal Disponivel,
    decimal EstoqueMinimo,
    bool AbaixoDoEstoqueMinimo)
{
    public static ItemEstoqueResponse De(ItemEstoque item, string sku, string descricao) => new(
        item.Id.Valor,
        item.PecaId.Valor,
        sku,
        descricao,
        item.Unidade,
        item.Saldo.Valor,
        item.Reservado.Valor,
        item.Disponivel.Valor,
        item.EstoqueMinimo.Valor,
        item.AbaixoDoEstoqueMinimo);
}

public sealed record ItemEstoqueDetalhadoResponse(
    ItemEstoqueResponse Resumo,
    IReadOnlyList<ReservaDto> Reservas,
    IReadOnlyList<MovimentoDto> Movimentos);

public sealed record DisponibilidadeDto(
    Guid PecaId,
    string Sku,
    string Descricao,
    decimal QuantidadeSolicitada,
    decimal Disponivel,
    bool Atende);
