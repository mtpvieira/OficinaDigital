using System.ComponentModel.DataAnnotations;
using OficinaDigital.Domain.Orcamentos;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Application.Orcamentos;

public sealed record ResponderOrcamentoRequest([Required] DecisaoCliente Decisao);

public sealed record ItemOrcadoDto(
    TipoItemOrcado Tipo,
    Guid ReferenciaId,
    string Descricao,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public sealed record VersaoOrcamentoResponse(
    int Numero,
    decimal Total,
    DateTime GeradoEm,
    DateTime? EnviadoEm,
    bool Vigente,
    DecisaoCliente? Decisao,
    DateTime? RespondidoEm,
    IReadOnlyList<ItemOrcadoDto> Itens)
{
    public static VersaoOrcamentoResponse De(VersaoOrcamento v, bool vigente) => new(
        v.Numero,
        v.Total.Valor,
        v.GeradoEm,
        v.EnviadoEm,
        vigente,
        v.Resposta?.Decisao,
        v.Resposta?.RespondidoEm,
        v.Itens
            .Select(i => new ItemOrcadoDto(i.Tipo, i.ReferenciaId, i.Descricao, i.Quantidade,
                i.PrecoUnitario.Valor, i.Subtotal.Valor))
            .ToList());
}

public sealed record OrcamentoResponse(
    Guid Id,
    Guid OrdemDeServicoId,
    TipoOrcamento Tipo,
    int VersaoVigente,
    decimal TotalVigente,
    IReadOnlyList<VersaoOrcamentoResponse> Versoes)
{
    public static OrcamentoResponse De(Orcamento o) => new(
        o.Id.Valor,
        o.OrdemDeServicoId.Valor,
        o.Tipo,
        o.VersaoVigente,
        o.ObterVersaoVigente().Total.Valor,
        o.Versoes
            .OrderBy(v => v.Numero)
            .Select(v => VersaoOrcamentoResponse.De(v, v.Numero == o.VersaoVigente))
            .ToList());
}
