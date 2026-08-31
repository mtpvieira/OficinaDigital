using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Atendimento.Events;

public sealed record OsAberta(OrdemDeServicoId OrdemDeServicoId, ClienteId ClienteId, VeiculoId VeiculoId)
    : DomainEvent;

public sealed record DiagnosticoIniciado(OrdemDeServicoId OrdemDeServicoId) : DomainEvent;

public sealed record ServicosDaOsRegistrados(OrdemDeServicoId OrdemDeServicoId, int Quantidade) : DomainEvent;

public sealed record PecasDaOsRegistradas(
    OrdemDeServicoId OrdemDeServicoId,
    IReadOnlyList<(PecaId PecaId, decimal Quantidade)> Pecas) : DomainEvent;

public sealed record DiagnosticoFinalizado(OrdemDeServicoId OrdemDeServicoId, bool PossuiPecaPendenteDeCompra)
    : DomainEvent;

public sealed record ServicoIniciado(OrdemDeServicoId OrdemDeServicoId, DateTime IniciadoEm) : DomainEvent;

public sealed record ProblemaAdicionalRegistrado(OrdemDeServicoId OrdemDeServicoId, string Descricao) : DomainEvent;

public sealed record ServicosEPecasAdicionaisRegistrados(OrdemDeServicoId OrdemDeServicoId) : DomainEvent;

public sealed record ItensAdicionaisRemovidosDaOs(OrdemDeServicoId OrdemDeServicoId) : DomainEvent;

public sealed record ServicoConcluido(OrdemDeServicoId OrdemDeServicoId, DateTime ConcluidoEm) : DomainEvent;

public sealed record DuracaoRealDaExecucaoRegistrada(OrdemDeServicoId OrdemDeServicoId, int DuracaoEmMinutos)
    : DomainEvent;

public sealed record VeiculoEntregue(OrdemDeServicoId OrdemDeServicoId, ClienteId ClienteId, VeiculoId VeiculoId)
    : DomainEvent;

public sealed record OsCancelada(OrdemDeServicoId OrdemDeServicoId, MotivoCancelamento Motivo, string? Observacao)
    : DomainEvent;

public sealed record StatusDaOsAlterado(
    OrdemDeServicoId OrdemDeServicoId,
    StatusOS StatusAnterior,
    StatusOS NovoStatus) : DomainEvent;
