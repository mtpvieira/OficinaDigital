using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Domain.Orcamentos.Events;

public sealed record OrcamentoGerado(
    OrcamentoId OrcamentoId,
    OrdemDeServicoId OrdemDeServicoId,
    TipoOrcamento Tipo,
    int Versao,
    decimal Total) : DomainEvent;

public sealed record OrcamentoAlterado(OrcamentoId OrcamentoId, OrdemDeServicoId OrdemDeServicoId, int NovaVersao)
    : DomainEvent;

public sealed record OrcamentoEnviadoAoCliente(
    OrcamentoId OrcamentoId,
    OrdemDeServicoId OrdemDeServicoId,
    TipoOrcamento Tipo,
    int Versao) : DomainEvent;

public sealed record OrcamentoAprovado(
    OrcamentoId OrcamentoId,
    OrdemDeServicoId OrdemDeServicoId,
    TipoOrcamento Tipo,
    int Versao) : DomainEvent;

public sealed record OrcamentoReprovado(
    OrcamentoId OrcamentoId,
    OrdemDeServicoId OrdemDeServicoId,
    TipoOrcamento Tipo,
    int Versao) : DomainEvent;

