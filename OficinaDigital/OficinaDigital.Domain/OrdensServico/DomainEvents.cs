using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.OrdensServico;

public sealed record OSCriada(Guid OrdemServicoId, string Numero, DateTime OcorridoEm) : IDomainEvent;

public sealed record OrcamentoEnviado(Guid OrdemServicoId, string Numero, Money Orcamento, DateTime OcorridoEm) : IDomainEvent;

public sealed record OSAprovada(Guid OrdemServicoId, string Numero, DateTime OcorridoEm) : IDomainEvent;

public sealed record ExecucaoIniciada(Guid OrdemServicoId, string Numero, DateTime OcorridoEm) : IDomainEvent;

public sealed record OSFinalizada(Guid OrdemServicoId, string Numero, DateTime OcorridoEm) : IDomainEvent;

public sealed record OSEntregue(Guid OrdemServicoId, string Numero, DateTime OcorridoEm) : IDomainEvent;
